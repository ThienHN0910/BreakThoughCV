using BreakThroughCV.API.Services;
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
    private readonly CloudinaryService _cloudinary;
    private readonly MongoDbService _db;

    public CvController(CloudinaryService cloudinary, MongoDbService db)
    {
        _cloudinary = cloudinary;
        _db = db;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string GetRole() => User.FindFirst("role")?.Value ?? "none";

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
            var url = await _cloudinary.UploadFileAsync(cvFile, "cvs");
            if (url == null)
                return StatusCode(500, new { message = "Failed to upload CV" });

            // Save CV URL to user
            var userId = GetUserId();
            var update = Builders<Models.User>.Update.Set(u => u.CvUrl, url);
            var result = await _db.Users.UpdateOneAsync(u => u.Id == userId, update);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                message = "CV uploaded successfully",
                cvUrl = url
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
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
                cvUrl = user.CvUrl,
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
                cvUrl = user.CvUrl
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

