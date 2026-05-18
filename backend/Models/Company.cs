using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BreakThroughCV.API.Models;

public class Company
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("recruiterId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RecruiterId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("logoUrl")]
    public string? LogoUrl { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("categoryId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CategoryId { get; set; }

    [BsonElement("website")]
    public string? Website { get; set; }
}
