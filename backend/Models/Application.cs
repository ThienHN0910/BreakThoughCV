using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BreakThroughCV.API.Models;

public class Application
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("jobId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string JobId { get; set; } = string.Empty;

    [BsonElement("candidateId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CandidateId { get; set; } = string.Empty;

    [BsonElement("cvUrl")]
    public string CvUrl { get; set; } = string.Empty;

    [BsonElement("appliedAt")]
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("status")]
    public string Status { get; set; } = "Pending"; // "Pending" | "Reviewed"
}
