using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly CloudinaryService _cloudinary;

    public CompanyController(MongoDbService db, CloudinaryService cloudinary)
    {
        _db = db;
        _cloudinary = cloudinary;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private bool IsRecruiter() => User.FindFirst("role")?.Value == "recruiter";

    [HttpGet("my")]
    public async Task<IActionResult> GetMyCompany()
    {
        if (!IsRecruiter()) return Forbid();
        var userId = GetUserId();
        var company = await _db.Companies.Find(c => c.RecruiterId == userId).FirstOrDefaultAsync();
        if (company == null) return NotFound(new { message = "No company found" });

        return Ok(MapToDto(company));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id)
    {
        var company = await _db.Companies.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (company == null) return NotFound();
        return Ok(MapToDto(company));
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromForm] UpsertCompanyRequest request, IFormFile? logo)
    {
        if (!IsRecruiter()) return Forbid();
        var userId = GetUserId();
        var existing = await _db.Companies.Find(c => c.RecruiterId == userId).FirstOrDefaultAsync();

        string? logoUrl = existing?.LogoUrl;
        if (logo != null)
        {
            if (!FileValidationService.IsValidImage(logo, out var validationError))
                return BadRequest(new { message = validationError });

            logoUrl = await _cloudinary.UploadImageAsync(logo, "company-logos");
            if (logoUrl == null) return StatusCode(500, new { message = "Failed to upload logo" });
        }

        if (existing == null)
        {
            var company = new Company
            {
                RecruiterId = userId,
                Name = request.Name,
                LogoUrl = logoUrl,
                Description = request.Description,
                CategoryId = request.CategoryId,
                Website = request.Website
            };
            await _db.Companies.InsertOneAsync(company);
            return CreatedAtAction(nameof(GetMyCompany), MapToDto(company));
        }
        else
        {
            var update = Builders<Company>.Update
                .Set(c => c.Name, request.Name)
                .Set(c => c.Description, request.Description)
                .Set(c => c.CategoryId, request.CategoryId)
                .Set(c => c.Website, request.Website)
                .Set(c => c.LogoUrl, logoUrl);
            await _db.Companies.UpdateOneAsync(c => c.Id == existing.Id, update);
            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.CategoryId = request.CategoryId;
            existing.Website = request.Website;
            existing.LogoUrl = logoUrl;
            return Ok(MapToDto(existing));
        }
    }

    private static CompanyResponse MapToDto(Company c) => new(
        c.Id!, c.RecruiterId, c.Name, c.LogoUrl, c.Description, c.CategoryId, c.Website
    );
}
