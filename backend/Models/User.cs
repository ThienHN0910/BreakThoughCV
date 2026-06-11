using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BreakThroughCV.API.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [BsonElement("cvUrl")]
    public string? CvUrl { get; set; }

    [BsonElement("role")]
    public string Role { get; set; } = "none"; // "none" | "candidate" | "recruiter" | "admin"

    [BsonElement("isActive")]
    [BsonDefaultValue(true)]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [BsonElement("cvUploadCount")]
    [BsonDefaultValue(0)]
    public int CvUploadCount { get; set; }

    [BsonElement("aiReviewCount")]
    [BsonDefaultValue(0)]
    public int AiReviewCount { get; set; }

    [BsonElement("aiAccessPaidAt")]
    public DateTime? AiAccessPaidAt { get; set; }

    [BsonElement("aiAccessExpiresAt")]
    public DateTime? AiAccessExpiresAt { get; set; }
}
