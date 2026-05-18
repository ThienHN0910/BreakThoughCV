using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BreakThroughCV.API.Models;

public class CvReview
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("candidateId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CandidateId { get; set; } = string.Empty;

    [BsonElement("jobId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string JobId { get; set; } = string.Empty;

    [BsonElement("score")]
    public int Score { get; set; }

    [BsonElement("missingKeywords")]
    public List<string> MissingKeywords { get; set; } = new();

    [BsonElement("criticalFixes")]
    public List<string> CriticalFixes { get; set; } = new();

    [BsonElement("tailoredSuggestions")]
    public List<TailoredSuggestion> TailoredSuggestions { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TailoredSuggestion
{
    [BsonElement("section")]
    public string Section { get; set; } = string.Empty;

    [BsonElement("originalText")]
    public string OriginalText { get; set; } = string.Empty;

    [BsonElement("suggestedText")]
    public string SuggestedText { get; set; } = string.Empty;
}
