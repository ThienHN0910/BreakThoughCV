namespace BreakThroughCV.API.DTOs;

public record CreateWebsiteReviewRequest(
    int Rating,
    string? Comment
);

public record WebsiteReviewResponse(
    string Id,
    int Rating,
    string Comment,
    DateTime CreatedAt
);

public record AdminWebsiteReviewResponse(
    string Id,
    string UserId,
    string UserName,
    string UserEmail,
    string UserRole,
    int Rating,
    string Comment,
    DateTime CreatedAt
);

public record PublicWebsiteReviewResponse(
    string Id,
    string UserName,
    string UserRole,
    int Rating,
    string Comment,
    DateTime CreatedAt
);

public record WebsiteReviewStatsResponse(
    int TotalReviews,
    double AverageRating
);
