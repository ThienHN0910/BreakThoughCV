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
    private string GetRole() => User.FindFirst("role")?.Value ?? "none";

    [HttpPost("upload")]
    public async Task<IActionResult> UploadCv(IFormFile cvFile)
    {
        if (GetRole() != "candidate") return Forbid();
        if (!FileValidationService.IsValidCv(cvFile, out var validationError))
            return BadRequest(new { message = validationError });

        var url = await _cloudinary.UploadFileAsync(cvFile, "cvs");
        if (url == null) return StatusCode(500, new { message = "Failed to upload CV" });

        return Ok(new { url });
    }
}
