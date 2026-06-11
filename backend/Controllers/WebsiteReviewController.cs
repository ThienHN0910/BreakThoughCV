using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/website-reviews")]
[Authorize]
public class WebsiteReviewController : ControllerBase
{
    private readonly MongoDbService _db;

    public WebsiteReviewController(MongoDbService db)
    {
        _db = db;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string GetRole() => User.FindFirst("role")?.Value ?? "none";
    private bool CanReviewWebsite() => GetRole() == "candidate" || GetRole() == "recruiter";

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLatestReviews([FromQuery] int limit = 6)
    {
        limit = Math.Clamp(limit, 1, 20);

        var reviews = await _db.WebsiteReviews
            .Find(_ => true)
            .SortByDescending(r => r.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
        var users = await _db.Users.Find(u => userIds.Contains(u.Id!)).ToListAsync();
        var userMap = users.ToDictionary(u => u.Id!, u => u);

        var items = reviews.Select(review =>
        {
            userMap.TryGetValue(review.UserId, out var user);
            return new PublicWebsiteReviewResponse(
                Id: review.Id!,
                UserName: user?.Name ?? "Người dùng",
                UserRole: user?.Role ?? "user",
                Rating: review.Rating,
                Comment: review.Comment,
                CreatedAt: review.CreatedAt
            );
        });

        return Ok(items);
    }

    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStats()
    {
        var reviews = await _db.WebsiteReviews
            .Find(_ => true)
            .Project(r => r.Rating)
            .ToListAsync();

        var totalReviews = reviews.Count;
        var averageRating = totalReviews == 0 ? 0 : Math.Round(reviews.Average(), 1);

        return Ok(new WebsiteReviewStatsResponse(totalReviews, averageRating));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyReviews()
    {
        if (!CanReviewWebsite()) return Forbid();

        var userId = GetUserId();
        var reviews = await _db.WebsiteReviews
            .Find(r => r.UserId == userId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews.Select(ToResponse));
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateWebsiteReviewRequest request)
    {
        if (!CanReviewWebsite()) return Forbid();
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Rating must be between 1 and 5" });

        var review = new WebsiteReview
        {
            UserId = GetUserId(),
            Rating = request.Rating,
            Comment = request.Comment?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _db.WebsiteReviews.InsertOneAsync(review);
        return CreatedAtAction(nameof(GetMyReviews), ToResponse(review));
    }

    private static WebsiteReviewResponse ToResponse(WebsiteReview review)
    {
        return new WebsiteReviewResponse(
            Id: review.Id!,
            Rating: review.Rating,
            Comment: review.Comment,
            CreatedAt: review.CreatedAt
        );
    }
}
