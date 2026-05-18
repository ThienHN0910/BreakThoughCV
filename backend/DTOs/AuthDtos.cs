namespace BreakThroughCV.API.DTOs;

public record GoogleLoginRequest(string IdToken);

public record UpdateRoleRequest(string Role);

public record AuthResponse(
    string? Token,
    string UserId,
    string Email,
    string Name,
    string? AvatarUrl,
    string Role,
    bool IsNewUser
);
