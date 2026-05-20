namespace BreakThroughCV.API.DTOs;

public record JobSuggestionRequest(string? CvText, string? CvUrl);

public record JobSuggestionResult(string JobId, string Reason);

public record CvReviewRequest(string JobId, string? CvText, string? CvUrl);

public record CvReviewResponse(
    string Id,
    int Score,
    List<string> MissingKeywords,
    List<string> CriticalFixes,
    List<TailoredSuggestionDto> TailoredSuggestions,
    DateTime CreatedAt
);

public record TailoredSuggestionDto(string Section, string OriginalText, string SuggestedText);
