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
        var payload = await _googleAuth.ValidateTokenAsync(request.IdToken);
        if (payload == null) return Unauthorized(new { message = "Invalid Google token" });

        var existingUser = await _db.Users.Find(u => u.Email == payload.Email).FirstOrDefaultAsync();

        if (existingUser == null)
        {
            var newUser = new User
            {
                Email = payload.Email,
                Name = payload.Name,
                AvatarUrl = payload.Picture,
                Role = "none",
                CreatedAt = DateTime.UtcNow
            };
            await _db.Users.InsertOneAsync(newUser);
            return Ok(new AuthResponse(
                Token: string.Empty,
                UserId: newUser.Id!,
                Email: newUser.Email,
                Name: newUser.Name,
                AvatarUrl: newUser.AvatarUrl,
                Role: newUser.Role,
                IsNewUser: true
            ));
        }

        var token = _jwtService.GenerateToken(existingUser);
        return Ok(new AuthResponse(
            Token: token,
            UserId: existingUser.Id!,
            Email: existingUser.Email,
            Name: existingUser.Name,
            AvatarUrl: existingUser.AvatarUrl,
            Role: existingUser.Role,
            IsNewUser: false
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
            IsNewUser: false
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
            role = user.Role
        });
    }
}
