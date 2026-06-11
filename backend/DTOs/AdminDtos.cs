namespace BreakThroughCV.API.DTOs;

public record AdminUserDto(
    string Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    int CvUploadCount,
    int AiReviewCount,
    bool AiAccessEnabled,
    DateTime? AiAccessExpiresAt
);

public record AdminUserListResponse(
    IReadOnlyList<AdminUserDto> Items,
    int Total,
    int Page,
    int PageSize
);

public record AdminUserStatsResponse(
    int TotalUsers,
    int CandidateCount,
    int RecruiterCount,
    int AdminCount,
    int NoneRoleCount
);

public record AdminUpdateRoleRequest(string Role);

public record AdminUpdateStatusRequest(bool IsActive);
