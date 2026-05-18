namespace BreakThroughCV.API.DTOs;

public record UpsertCompanyRequest(
    string Name,
    string? Description,
    string? CategoryId,
    string? Website
);

public record CompanyResponse(
    string Id,
    string RecruiterId,
    string Name,
    string? LogoUrl,
    string? Description,
    string? CategoryId,
    string? Website
);

public record CreateCategoryRequest(string Name, string Slug);
