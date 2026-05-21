using BreakThroughCV.API.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/cv")]
[Authorize]
public class CvController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly IWebHostEnvironment _environment;
    private readonly CloudinaryService _cloudinary;
    private readonly IHttpClientFactory _httpClientFactory;

    public CvController(MongoDbService db, IWebHostEnvironment environment, CloudinaryService cloudinary, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _environment = environment;
        _cloudinary = cloudinary;
        _httpClientFactory = httpClientFactory;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string GetRole() => User.FindFirst("role")?.Value ?? "none";

    private string GetCvPublicUrl(string userId)
    {
        return $"{Request.Scheme}://{Request.Host}/api/cv/file/{userId}";
    }

    [HttpGet("preview/{id}")]
    public async Task<IActionResult> PreviewCv(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Invalid id" });

        try
        {
            // Authorization scope:
            // - candidate: only preview their own user CV (id == userId)
            // - recruiter: only preview CVs by applicationId that belongs to their company/job
            string? targetUrl = null;

            var role = GetRole();
            if (role == "candidate")
            {
                var userId = GetUserId();
                if (!string.Equals(id, userId, StringComparison.Ordinal))
                    return Forbid();

                var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
                if (user != null && !string.IsNullOrWhiteSpace(user.CvUrl))
                    targetUrl = user.CvUrl;
            }
            else if (role == "recruiter")
            {
                var recruiterId = GetUserId();
                var application = await _db.Applications.Find(a => a.Id == id).FirstOrDefaultAsync();
                if (application == null) return NotFound(new { message = "Application not found" });

                var company = await _db.Companies.Find(c => c.RecruiterId == recruiterId).FirstOrDefaultAsync();
                if (company == null) return Forbid();

                var job = await _db.Jobs.Find(j => j.Id == application.JobId && j.CompanyId == company.Id).FirstOrDefaultAsync();
                if (job == null) return Forbid();

                if (!string.IsNullOrWhiteSpace(application.CvUrl))
                    targetUrl = application.CvUrl;
            }
            else
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(targetUrl))
                return NotFound(new { message = "CV file not found" });

            var client = _httpClientFactory.CreateClient();

            HttpResponseMessage? resp = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);

                // Forward Range header if present (useful for some PDF viewers)
                if (Request.Headers.ContainsKey("Range"))
                {
                    var range = Request.Headers["Range"].ToString();
                    request.Headers.TryAddWithoutValidation("Range", range);
                }

                resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                // If remote returned 401 and it's a Cloudinary URL, try admin download
                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized && targetUrl.Contains("res.cloudinary.com"))
                {
                    var cloudStream = await _cloudinary.DownloadFileAsync(targetUrl);
                    if (cloudStream != null)
                    {
                        resp.Dispose();
                        return File(cloudStream, "application/pdf");
                    }
                }

                if (!resp.IsSuccessStatusCode)
                {
                    resp.Dispose();
                    return BadRequest(new { message = "Failed to download CV file" });
                }

                var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/pdf";
                var stream = await resp.Content.ReadAsStreamAsync();

                // Ensure the HttpResponseMessage stays alive until ASP.NET finishes streaming.
                HttpContext.Response.RegisterForDispose(resp);

                return File(stream, contentType);
            }
            catch (HttpRequestException)
            {
                return BadRequest(new { message = "Failed to fetch CV file" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Unexpected error", error = ex.Message });
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadCv(IFormFile? cvFile)
    {
        if (GetRole() != "candidate")
            return Forbid();

        if (cvFile == null)
            return BadRequest(new { message = "No file uploaded" });

        if (!FileValidationService.IsValidCv(cvFile, out var validationError))
            return BadRequest(new { message = validationError });

        try
        {
            // Upload raw file to Cloudinary (do not persist locally)
            var uploadedUrl = await _cloudinary.UploadFileAsync(cvFile, "cvs");
            if (uploadedUrl == null)
                return StatusCode(500, new { message = "Failed to upload to storage provider" });

            var userId = GetUserId();
            var update = Builders<Models.User>.Update.Set(u => u.CvUrl, uploadedUrl);
            var result = await _db.Users.UpdateOneAsync(u => u.Id == userId, update);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                message = "CV uploaded successfully",
                cvUrl = GetCvPublicUrl(userId),
                rawCvUrl = uploadedUrl,
                hasCV = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("file/{userId}")]
    [AllowAnonymous]
    public async Task GetCvFile(string userId)
    {
        // Proxy the CV URL stored in user profile and stream it back to the client.
        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null || string.IsNullOrEmpty(user.CvUrl))
        {
            Response.StatusCode = 404;
            await Response.WriteAsJsonAsync(new { message = "CV file not found" });
            return;
        }

        var targetUrl = user.CvUrl;
        var client = _httpClientFactory.CreateClient();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);

            // Forward Range header if present (for partial requests)
            if (Request.Headers.ContainsKey("Range"))
            {
                var range = Request.Headers["Range"].ToString();
                request.Headers.TryAddWithoutValidation("Range", range);
            }

            var resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            // If remote returned 401 and it's a Cloudinary URL, try admin download
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized && targetUrl.Contains("res.cloudinary.com"))
            {
                var cloudStream = await _cloudinary.DownloadFileAsync(targetUrl);
                if (cloudStream != null)
                {
                    Response.StatusCode = 200;
                    Response.ContentType = "application/pdf";
                    Response.Headers["Accept-Ranges"] = "bytes";
                    Response.ContentLength = cloudStream.Length;
                    await cloudStream.CopyToAsync(Response.Body);
                    return;
                }
            }

            Response.StatusCode = (int)resp.StatusCode;

            // Copy selected headers
            if (resp.Content.Headers.ContentType != null)
                Response.ContentType = resp.Content.Headers.ContentType.ToString();

            if (resp.Headers.TryGetValues("Accept-Ranges", out var acceptRanges))
                Response.Headers["Accept-Ranges"] = acceptRanges.ToArray();

            if (resp.Content.Headers.ContentLength.HasValue)
                Response.ContentLength = resp.Content.Headers.ContentLength.Value;

            if (resp.Content.Headers.ContentRange != null)
                Response.Headers["Content-Range"] = resp.Content.Headers.ContentRange.ToString();

            foreach (var header in resp.Content.Headers)
            {
                // avoid overwriting content-type which was already set
                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    continue;

                Response.Headers[header.Key] = header.Value.ToArray();
            }

            // Stream content directly
            await resp.Content.CopyToAsync(Response.Body);
        }
        catch (HttpRequestException ex)
        {
            Response.StatusCode = 502;
            await Response.WriteAsJsonAsync(new { message = "Failed to fetch remote CV file", error = ex.Message });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyCv()
    {
        try
        {
            var userId = GetUserId();
            var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                cvUrl = string.IsNullOrEmpty(user.CvUrl) ? null : GetCvPublicUrl(userId),
                rawCvUrl = string.IsNullOrEmpty(user.CvUrl) ? null : user.CvUrl,
                hasCV = !string.IsNullOrEmpty(user.CvUrl)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserCv(string userId)
    {
        try
        {
            var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null || string.IsNullOrEmpty(user.CvUrl))
                return NotFound(new { message = "CV not found" });

            return Ok(new
            {
                cvUrl = GetCvPublicUrl(userId),
                rawCvUrl = user.CvUrl
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCv()
    {
        try
        {
            var userId = GetUserId();

            // Attempt to delete remote file from provider if present
            var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return NotFound(new { message = "User not found" });

            if (!string.IsNullOrEmpty(user.CvUrl))
            {
                try
                {
                    var deleted = await _cloudinary.DeleteFileByUrlAsync(user.CvUrl);
                    if (!deleted)
                        _cloudinary.GetType(); // no-op to keep compiler happy; fallback: just log (logger in service already logs)
                }
                catch
                {
                    // ignore provider delete failures; proceed to clear DB reference
                }
            }

            var update = Builders<Models.User>.Update.Set(u => u.CvUrl, null);
            var result = await _db.Users.UpdateOneAsync(u => u.Id == userId, update);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "CV deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
