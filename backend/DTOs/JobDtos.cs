namespace BreakThroughCV.API.DTOs;

public record CreateJobRequest(
    string Title,
    string? CategoryId,
    string Description,
    List<string> Responsibilities,
    List<string> MustHaveSkills,
    List<string> NiceToHaveSkills,
    int MinExperienceYears
);

public record JobResponse(
    string Id,
    string CompanyId,
    string CompanyName,
    string? CompanyLogoUrl,
    string Title,
    string? CategoryId,
    string? CategoryName,
    string Description,
    List<string> Responsibilities,
    List<string> MustHaveSkills,
    List<string> NiceToHaveSkills,
    int MinExperienceYears,
    DateTime CreatedAt
);
