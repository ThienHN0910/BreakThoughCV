using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly GeminiService _gemini;
    private readonly PdfTextService _pdfTextService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AIController(MongoDbService db, GeminiService gemini, PdfTextService pdfTextService, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _gemini = gemini;
        _pdfTextService = pdfTextService;
        _httpClientFactory = httpClientFactory;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private bool IsCandidate() => User.FindFirst("role")?.Value == "candidate";

    [HttpPost("suggest-jobs")]
    public async Task<IActionResult> SuggestJobs([FromBody] JobSuggestionRequest request)
    {
        if (!IsCandidate()) return Forbid();
        string? cvText = request.CvText;
        if (string.IsNullOrWhiteSpace(cvText) && !string.IsNullOrWhiteSpace(request.CvUrl))
        {
            // fetch URL and extract text
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var resp = await client.GetAsync(request.CvUrl);
                if (resp.IsSuccessStatusCode)
                {
                    await using var stream = await resp.Content.ReadAsStreamAsync();
                    cvText = await _pdfTextService.ExtractTextAsync(stream);
                }
            }
            catch
            {
                // ignore and let cvText remain null
            }
        }

        if (string.IsNullOrWhiteSpace(cvText))
            return BadRequest(new { message = "CV text is required (provide cvText or cvUrl)" });

        var jobs = await _db.Jobs.Find(_ => true).Limit(50).ToListAsync();
        if (jobs.Count == 0) return Ok(new { suggestions = new List<object>() });

        var suggestions = await _gemini.SuggestJobsAsync(cvText, jobs);
        if (suggestions == null) return StatusCode(503, new { message = "AI service unavailable" });

        return Ok(new { suggestions });
    }

    [HttpPost("review-cv")]
    public async Task<IActionResult> ReviewCv([FromBody] CvReviewRequest request)
    {
        if (!IsCandidate()) return Forbid();
        string? cvText = request.CvText;
        if (string.IsNullOrWhiteSpace(cvText) && !string.IsNullOrWhiteSpace(request.CvUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var resp = await client.GetAsync(request.CvUrl);
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
            return BadRequest(new { message = "CV text is required (provide cvText or cvUrl)" });

        var job = await _db.Jobs.Find(j => j.Id == request.JobId).FirstOrDefaultAsync();
        if (job == null) return NotFound(new { message = "Job not found" });

        var candidateId = GetUserId();
        var reviewResult = await _gemini.ReviewCvAsync(cvText, job);
        if (reviewResult == null) return StatusCode(503, new { message = "AI service unavailable" });

        var cvReview = new CvReview
        {
            CandidateId = candidateId,
            JobId = request.JobId,
            Score = reviewResult.Score,
            MissingKeywords = reviewResult.MissingKeywords,
            CriticalFixes = reviewResult.CriticalFixes,
            TailoredSuggestions = reviewResult.TailoredSuggestions.Select(s => new TailoredSuggestion
            {
                Section = s.Section,
                OriginalText = s.OriginalText,
                SuggestedText = s.SuggestedText
            }).ToList(),
            CreatedAt = DateTime.UtcNow
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

    [HttpGet("review-history")]
    public async Task<IActionResult> GetReviewHistory()
    {
        if (!IsCandidate()) return Forbid();
        var candidateId = GetUserId();
        var reviews = await _db.CvReviews.Find(r => r.CandidateId == candidateId)
            .SortByDescending(r => r.CreatedAt).ToListAsync();

        return Ok(reviews.Select(r => new CvReviewResponse(
            Id: r.Id!,
            Score: r.Score,
            MissingKeywords: r.MissingKeywords,
            CriticalFixes: r.CriticalFixes,
            TailoredSuggestions: r.TailoredSuggestions.Select(s => new TailoredSuggestionDto(
                s.Section, s.OriginalText, s.SuggestedText
            )).ToList(),
            CreatedAt: r.CreatedAt
        )));
    }
}
