using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly GoogleAuthService _googleAuth;
    private readonly JwtService _jwtService;

    public AuthController(MongoDbService db, GoogleAuthService googleAuth, JwtService jwtService)
    {
        _db = db;
        _googleAuth = googleAuth;
        _jwtService = jwtService;
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { message = "IdToken is required" });

        var payload = await _googleAuth.ValidateTokenAsync(request.IdToken);
        if (payload == null) return Unauthorized(new { message = "Invalid Google token" });

        var existingUser = await _db.Users.Find(u => u.Email == payload.Email).FirstOrDefaultAsync();

        if (existingUser != null && !existingUser.IsActive)
            return Unauthorized(new { message = "Tài khoản đã bị vô hiệu hóa" });

        if (existingUser == null)
        {
            var now = DateTime.UtcNow;
            var newUser = new User
            {
                Email = payload.Email,
                Name = payload.Name,
                AvatarUrl = payload.Picture,
                Role = "none",
                CreatedAt = now,
                LastLoginAt = now
            };
            await _db.Users.InsertOneAsync(newUser);
            var newUserToken = _jwtService.GenerateToken(newUser);
            return Ok(new AuthResponse(
                Token: newUserToken,
                UserId: newUser.Id!,
                Email: newUser.Email,
                Name: newUser.Name,
                AvatarUrl: newUser.AvatarUrl,
                Role: newUser.Role,
                IsNewUser: true,
                AiAccessEnabled: newUser.AiAccessPaidAt != null && (newUser.AiAccessExpiresAt == null || newUser.AiAccessExpiresAt > DateTime.UtcNow)
            ));
        }

        existingUser.LastLoginAt = DateTime.UtcNow;
        await _db.Users.UpdateOneAsync(
            u => u.Id == existingUser.Id,
            Builders<User>.Update.Set(u => u.LastLoginAt, existingUser.LastLoginAt)
        );

        var token = _jwtService.GenerateToken(existingUser);
        return Ok(new AuthResponse(
            Token: token,
            UserId: existingUser.Id!,
            Email: existingUser.Email,
            Name: existingUser.Name,
            AvatarUrl: existingUser.AvatarUrl,
            Role: existingUser.Role,
            IsNewUser: false,
            AiAccessEnabled: existingUser.AiAccessPaidAt != null && (existingUser.AiAccessExpiresAt == null || existingUser.AiAccessExpiresAt > DateTime.UtcNow)
        ));
    }

    [HttpPut("update-role")]
    [Authorize]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleRequest request)
    {
        if (request.Role != "candidate" && request.Role != "recruiter")
            return BadRequest(new { message = "Role must be 'candidate' or 'recruiter'" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var currentRole = User.FindFirst("role")?.Value;
        if (currentRole == "admin")
            return BadRequest(new { message = "Admin cannot change role via this endpoint" });

        var update = Builders<User>.Update.Set(u => u.Role, request.Role);
        var result = await _db.Users.UpdateOneAsync(u => u.Id == userId, update);

        if (result.ModifiedCount == 0) return NotFound(new { message = "User not found" });

        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        var token = _jwtService.GenerateToken(user!);

        return Ok(new AuthResponse(
            Token: token,
            UserId: user!.Id!,
            Email: user.Email,
            Name: user.Name,
            AvatarUrl: user.AvatarUrl,
            Role: user.Role,
            IsNewUser: false,
            AiAccessEnabled: user.AiAccessPaidAt != null && (user.AiAccessExpiresAt == null || user.AiAccessExpiresAt > DateTime.UtcNow)
        ));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return NotFound();

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            name = user.Name,
            avatarUrl = user.AvatarUrl,
            role = user.Role,
            aiAccessEnabled = user.AiAccessPaidAt != null && (user.AiAccessExpiresAt == null || user.AiAccessExpiresAt > DateTime.UtcNow),
            aiAccessExpiresAt = user.AiAccessExpiresAt
        });
    }
}
