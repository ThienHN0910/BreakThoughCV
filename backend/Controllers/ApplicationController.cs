using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly CloudinaryService _cloudinary;

    public ApplicationController(MongoDbService db, CloudinaryService cloudinary)
    {
        _db = db;
        _cloudinary = cloudinary;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpPost]
    public async Task<IActionResult> Apply([FromForm] ApplyJobRequest request, IFormFile cvFile)
    {
        var candidateId = GetUserId();

        var existing = await _db.Applications.Find(
            a => a.JobId == request.JobId && a.CandidateId == candidateId
        ).FirstOrDefaultAsync();
        if (existing != null) return Conflict(new { message = "You have already applied to this job" });

        var cvUrl = await _cloudinary.UploadFileAsync(cvFile, "cvs");
        if (cvUrl == null) return StatusCode(500, new { message = "Failed to upload CV" });

        var application = new Application
        {
            JobId = request.JobId,
            CandidateId = candidateId,
            CvUrl = cvUrl,
            AppliedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        await _db.Applications.InsertOneAsync(application);
        return CreatedAtAction(nameof(GetMyApplications), application);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyApplications()
    {
        var candidateId = GetUserId();
        var applications = await _db.Applications.Find(a => a.CandidateId == candidateId)
            .SortByDescending(a => a.AppliedAt).ToListAsync();

        var enriched = await EnrichApplicationsAsync(applications);
        return Ok(enriched);
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetByJob(string jobId)
    {
        var recruiterId = GetUserId();
        var company = await _db.Companies.Find(c => c.RecruiterId == recruiterId).FirstOrDefaultAsync();
        if (company == null) return Forbid();

        var job = await _db.Jobs.Find(j => j.Id == jobId && j.CompanyId == company.Id).FirstOrDefaultAsync();
        if (job == null) return Forbid();

        var applications = await _db.Applications.Find(a => a.JobId == jobId)
            .SortByDescending(a => a.AppliedAt).ToListAsync();

        var enriched = await EnrichApplicationsAsync(applications);
        return Ok(enriched);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateApplicationStatusRequest request)
    {
        if (request.Status != "Pending" && request.Status != "Reviewed")
            return BadRequest(new { message = "Invalid status" });

        var recruiterId = GetUserId();
        var application = await _db.Applications.Find(a => a.Id == id).FirstOrDefaultAsync();
        if (application == null) return NotFound();

        var company = await _db.Companies.Find(c => c.RecruiterId == recruiterId).FirstOrDefaultAsync();
        if (company == null) return Forbid();
        var job = await _db.Jobs.Find(j => j.Id == application.JobId && j.CompanyId == company.Id).FirstOrDefaultAsync();
        if (job == null) return Forbid();

        var update = Builders<Application>.Update.Set(a => a.Status, request.Status);
        await _db.Applications.UpdateOneAsync(a => a.Id == id, update);
        return NoContent();
    }

    private async Task<List<ApplicationResponse>> EnrichApplicationsAsync(List<Application> applications)
    {
        var jobIds = applications.Select(a => a.JobId).Distinct().ToList();
        var candidateIds = applications.Select(a => a.CandidateId).Distinct().ToList();

        var jobs = await _db.Jobs.Find(j => jobIds.Contains(j.Id!)).ToListAsync();
        var candidates = await _db.Users.Find(u => candidateIds.Contains(u.Id!)).ToListAsync();

        var jobMap = jobs.ToDictionary(j => j.Id!, j => j);
        var candidateMap = candidates.ToDictionary(c => c.Id!, c => c);

        return applications.Select(a => {
            jobMap.TryGetValue(a.JobId, out var job);
            candidateMap.TryGetValue(a.CandidateId, out var candidate);
            return new ApplicationResponse(
                Id: a.Id!,
                JobId: a.JobId,
                JobTitle: job?.Title ?? "Unknown",
                CandidateId: a.CandidateId,
                CandidateName: candidate?.Name ?? "Unknown",
                CandidateEmail: candidate?.Email,
                CvUrl: a.CvUrl,
                AppliedAt: a.AppliedAt,
                Status: a.Status
            );
        }).ToList();
    }
}
