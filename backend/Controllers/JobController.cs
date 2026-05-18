using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobController : ControllerBase
{
    private readonly MongoDbService _db;

    public JobController(MongoDbService db)
    {
        _db = db;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? categoryId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var filterBuilder = Builders<Job>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(categoryId))
            filter &= filterBuilder.Eq(j => j.CategoryId, categoryId);

        if (!string.IsNullOrEmpty(keyword))
        {
            var regex = new MongoDB.Bson.BsonRegularExpression(keyword, "i");
            var keywordFilter = filterBuilder.Or(
                filterBuilder.Regex(j => j.Title, regex),
                filterBuilder.Regex(j => j.Description, regex),
                filterBuilder.AnyStringIn(j => j.MustHaveSkills, new MongoDB.Bson.BsonRegularExpression(keyword, "i")),
                filterBuilder.AnyStringIn(j => j.NiceToHaveSkills, new MongoDB.Bson.BsonRegularExpression(keyword, "i"))
            );
            filter &= keywordFilter;
        }

        var totalCount = await _db.Jobs.CountDocumentsAsync(filter);
        var jobs = await _db.Jobs.Find(filter)
            .SortByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var jobResponses = await EnrichJobsAsync(jobs);
        return Ok(new { totalCount, page, pageSize, data = jobResponses });
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id)
    {
        var job = await _db.Jobs.Find(j => j.Id == id).FirstOrDefaultAsync();
        if (job == null) return NotFound();
        var enriched = await EnrichJobsAsync(new List<Job> { job });
        return Ok(enriched.First());
    }

    [HttpGet("company/{companyId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCompany(string companyId)
    {
        var jobs = await _db.Jobs.Find(j => j.CompanyId == companyId)
            .SortByDescending(j => j.CreatedAt).ToListAsync();
        var enriched = await EnrichJobsAsync(jobs);
        return Ok(enriched);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        var userId = GetUserId();
        var company = await _db.Companies.Find(c => c.RecruiterId == userId).FirstOrDefaultAsync();
        if (company == null) return BadRequest(new { message = "You need to create a company first" });

        var job = new Job
        {
            CompanyId = company.Id!,
            Title = request.Title,
            CategoryId = request.CategoryId,
            Description = request.Description,
            Responsibilities = request.Responsibilities,
            MustHaveSkills = request.MustHaveSkills,
            NiceToHaveSkills = request.NiceToHaveSkills,
            MinExperienceYears = request.MinExperienceYears,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Jobs.InsertOneAsync(job);
        var enriched = await EnrichJobsAsync(new List<Job> { job });
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, enriched.First());
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateJob(string id, [FromBody] CreateJobRequest request)
    {
        var userId = GetUserId();
        var company = await _db.Companies.Find(c => c.RecruiterId == userId).FirstOrDefaultAsync();
        if (company == null) return Forbid();

        var job = await _db.Jobs.Find(j => j.Id == id && j.CompanyId == company.Id).FirstOrDefaultAsync();
        if (job == null) return NotFound();

        var update = Builders<Job>.Update
            .Set(j => j.Title, request.Title)
            .Set(j => j.CategoryId, request.CategoryId)
            .Set(j => j.Description, request.Description)
            .Set(j => j.Responsibilities, request.Responsibilities)
            .Set(j => j.MustHaveSkills, request.MustHaveSkills)
            .Set(j => j.NiceToHaveSkills, request.NiceToHaveSkills)
            .Set(j => j.MinExperienceYears, request.MinExperienceYears);

        await _db.Jobs.UpdateOneAsync(j => j.Id == id, update);
        job.Title = request.Title;
        job.CategoryId = request.CategoryId;
        job.Description = request.Description;
        job.Responsibilities = request.Responsibilities;
        job.MustHaveSkills = request.MustHaveSkills;
        job.NiceToHaveSkills = request.NiceToHaveSkills;
        job.MinExperienceYears = request.MinExperienceYears;

        var enriched = await EnrichJobsAsync(new List<Job> { job });
        return Ok(enriched.First());
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteJob(string id)
    {
        var userId = GetUserId();
        var company = await _db.Companies.Find(c => c.RecruiterId == userId).FirstOrDefaultAsync();
        if (company == null) return Forbid();

        var result = await _db.Jobs.DeleteOneAsync(j => j.Id == id && j.CompanyId == company.Id);
        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }

    private async Task<List<JobResponse>> EnrichJobsAsync(List<Job> jobs)
    {
        var companyIds = jobs.Select(j => j.CompanyId).Distinct().ToList();
        var categoryIds = jobs.Where(j => j.CategoryId != null).Select(j => j.CategoryId!).Distinct().ToList();

        var companies = await _db.Companies.Find(c => companyIds.Contains(c.Id!)).ToListAsync();
        var categories = categoryIds.Count > 0
            ? await _db.Categories.Find(c => categoryIds.Contains(c.Id!)).ToListAsync()
            : new List<Models.Category>();

        var companyMap = companies.ToDictionary(c => c.Id!, c => c);
        var categoryMap = categories.ToDictionary(c => c.Id!, c => c);

        return jobs.Select(j => {
            companyMap.TryGetValue(j.CompanyId, out var company);
            categoryMap.TryGetValue(j.CategoryId ?? "", out var category);
            return new JobResponse(
                Id: j.Id!,
                CompanyId: j.CompanyId,
                CompanyName: company?.Name ?? "Unknown",
                CompanyLogoUrl: company?.LogoUrl,
                Title: j.Title,
                CategoryId: j.CategoryId,
                CategoryName: category?.Name,
                Description: j.Description,
                Responsibilities: j.Responsibilities,
                MustHaveSkills: j.MustHaveSkills,
                NiceToHaveSkills: j.NiceToHaveSkills,
                MinExperienceYears: j.MinExperienceYears,
                CreatedAt: j.CreatedAt
            );
        }).ToList();
    }
}
