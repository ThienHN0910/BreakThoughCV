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
    private readonly PdfTextService _pdfTextService;
    private readonly GeminiService _gemini;
    private readonly IHttpClientFactory _httpClientFactory;

    public ApplicationController(MongoDbService db, CloudinaryService cloudinary, PdfTextService pdfTextService, GeminiService gemini, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _cloudinary = cloudinary;
        _pdfTextService = pdfTextService;
        _gemini = gemini;
        _httpClientFactory = httpClientFactory;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string GetRole() => User.FindFirst("role")?.Value ?? "none";

    [HttpPost]
    public async Task<IActionResult> Apply([FromForm] ApplyJobRequest request, IFormFile cvFile)
    {
        if (GetRole() != "candidate") return Forbid();
        var candidateId = GetUserId();
        if (!FileValidationService.IsValidCv(cvFile, out var validationError))
            return BadRequest(new { message = validationError });

        var existing = await _db.Applications.Find(
            a => a.JobId == request.JobId && a.CandidateId == candidateId
        ).FirstOrDefaultAsync();
        if (existing != null) return Conflict(new { message = "You have already applied to this job" });

        // Extract text from uploaded CV for AI features
        string? extractedText = null;
        try
        {
            if (cvFile != null)
            {
                await using var s = cvFile.OpenReadStream();
                extractedText = await _pdfTextService.ExtractTextAsync(s);
            }
        }
        catch
        {
            // non-fatal: continue without extracted text
            extractedText = null;
        }

        var cvUrl = await _cloudinary.UploadFileAsync(cvFile, "cvs");
        if (cvUrl == null) return StatusCode(500, new { message = "Failed to upload CV" });

        var application = new Application
        {
            JobId = request.JobId,
            CandidateId = candidateId,
            CvUrl = cvUrl,
            CvText = extractedText,
            AppliedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        await _db.Applications.InsertOneAsync(application);
        return CreatedAtAction(nameof(GetMyApplications), application);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyApplications()
    {
        if (GetRole() != "candidate") return Forbid();
        var candidateId = GetUserId();
        var applications = await _db.Applications.Find(a => a.CandidateId == candidateId)
            .SortByDescending(a => a.AppliedAt).ToListAsync();

        var enriched = await EnrichApplicationsAsync(applications);
        return Ok(enriched);
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetByJob(string jobId)
    {
        if (GetRole() != "recruiter") return Forbid();
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
        if (GetRole() != "recruiter") return Forbid();
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

    [HttpPost("{id}/ai-review")]
    public async Task<IActionResult> ReviewApplicationByAi(string id)
    {
        if (GetRole() != "recruiter") return Forbid();

        var application = await _db.Applications.Find(a => a.Id == id).FirstOrDefaultAsync();
        if (application == null) return NotFound();

        // verify recruiter owns the company for this job
        var recruiterId = GetUserId();
        var job = await _db.Jobs.Find(j => j.Id == application.JobId).FirstOrDefaultAsync();
        if (job == null) return NotFound();
        var company = await _db.Companies.Find(c => c.Id == job.CompanyId && c.RecruiterId == recruiterId).FirstOrDefaultAsync();
        if (company == null) return Forbid();

        string? cvText = application.CvText;
        if (string.IsNullOrWhiteSpace(cvText) && !string.IsNullOrWhiteSpace(application.CvUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var resp = await client.GetAsync(application.CvUrl);
                if (resp.IsSuccessStatusCode)
                {
                    await using var stream = await resp.Content.ReadAsStreamAsync();
                    cvText = await _pdfTextService.ExtractTextAsync(stream);
                }
            }
            catch
            {
            }
        }

        if (string.IsNullOrWhiteSpace(cvText))
            return BadRequest(new { message = "CV text not available for AI review" });

        var reviewResult = await _gemini.ReviewCvAsync(cvText, job);
        if (reviewResult == null) return StatusCode(503, new { message = "AI service unavailable" });

        // persist review record with recruiter info
        var cvReview = new CvReview
        {
            CandidateId = application.CandidateId,
            JobId = application.JobId,
            Score = reviewResult.Score,
            MissingKeywords = reviewResult.MissingKeywords,
            CriticalFixes = reviewResult.CriticalFixes,
            TailoredSuggestions = reviewResult.TailoredSuggestions.Select(s => new TailoredSuggestion
            {
                Section = s.Section,
                OriginalText = s.OriginalText,
                SuggestedText = s.SuggestedText
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            RequestedByRecruiterId = recruiterId,
            ApplicationId = application.Id
        };

        await _db.CvReviews.InsertOneAsync(cvReview);

        return Ok(new CvReviewResponse(
            Id: cvReview.Id!,
            Score: cvReview.Score,
            MissingKeywords: cvReview.MissingKeywords,
            CriticalFixes: cvReview.CriticalFixes,
            TailoredSuggestions: cvReview.TailoredSuggestions.Select(s => new TailoredSuggestionDto(
                s.Section, s.OriginalText, s.SuggestedText
            )).ToList(),
            CreatedAt: cvReview.CreatedAt
        ));
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
