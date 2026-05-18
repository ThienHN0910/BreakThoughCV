using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("upload")]
    public async Task<IActionResult> UploadCv(IFormFile cvFile)
    {
        if (cvFile == null || cvFile.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var allowedTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
        if (!allowedTypes.Contains(cvFile.ContentType))
            return BadRequest(new { message = "Only PDF and DOCX files are allowed" });

        const long maxSize = 10 * 1024 * 1024; // 10MB
        if (cvFile.Length > maxSize)
            return BadRequest(new { message = "File size must not exceed 10MB" });

        var url = await _cloudinary.UploadFileAsync(cvFile, "cvs");
        if (url == null) return StatusCode(500, new { message = "Failed to upload CV" });

        return Ok(new { url });
    }
}
