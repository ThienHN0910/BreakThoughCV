namespace BreakThroughCV.API.DTOs;

public record ApplyJobRequest(string JobId);

public record ApplicationResponse(
    string Id,
    string JobId,
    string JobTitle,
    string CandidateId,
    string CandidateName,
    string? CandidateEmail,
    string CvUrl,
    DateTime AppliedAt,
    string Status
);

public record UpdateApplicationStatusRequest(string Status);
