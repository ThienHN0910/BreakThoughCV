using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        "none", "candidate", "recruiter", "admin"
    };

    private readonly MongoDbService _db;

    public AdminController(MongoDbService db)
    {
        _db = db;
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = Builders<User>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = search.Trim();
            var searchFilter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(pattern, "i")),
                Builders<User>.Filter.Regex(u => u.Name, new MongoDB.Bson.BsonRegularExpression(pattern, "i"))
            );
            filter = Builders<User>.Filter.And(filter, searchFilter);
        }

        if (!string.IsNullOrWhiteSpace(role) && AllowedRoles.Contains(role))
        {
            filter = Builders<User>.Filter.And(filter, Builders<User>.Filter.Eq(u => u.Role, role));
        }

        var total = (int)await _db.Users.CountDocumentsAsync(filter);
        var users = await _db.Users
            .Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var items = users.Select(MapUser).ToList();
        return Ok(new AdminUserListResponse(items, total, page, pageSize));
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(MapUser(user));
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] AdminUpdateRoleRequest request)
    {
        if (!AllowedRoles.Contains(request.Role))
            return BadRequest(new { message = "Invalid role" });

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id && request.Role != "admin")
            return BadRequest(new { message = "Cannot change your own admin role" });

        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

        var update = Builders<User>.Update.Set(u => u.Role, request.Role);
        await _db.Users.UpdateOneAsync(u => u.Id == id, update);

        user.Role = request.Role;
        return Ok(MapUser(user));
    }

    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] AdminUpdateStatusRequest request)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id && !request.IsActive)
            return BadRequest(new { message = "Cannot deactivate your own account" });

        var result = await _db.Users.UpdateOneAsync(
            u => u.Id == id,
            Builders<User>.Update.Set(u => u.IsActive, request.IsActive));

        if (result.MatchedCount == 0) return NotFound(new { message = "User not found" });

        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        return Ok(MapUser(user!));
    }

    private static AdminUserDto MapUser(User user)
    {
        var aiAccessEnabled = user.AiAccessPaidAt != null
            && (user.AiAccessExpiresAt == null || user.AiAccessExpiresAt > DateTime.UtcNow);

        return new AdminUserDto(
            user.Id!,
            user.Email,
            user.Name,
            user.AvatarUrl,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            aiAccessEnabled,
            user.AiAccessExpiresAt
        );
    }
}
