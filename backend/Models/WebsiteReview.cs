using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BreakThroughCV.API.Models;

public class WebsiteReview
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; set; }

    [BsonElement("comment")]
    public string Comment { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
