using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BreakThroughCV.API.Models;

public class Job
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("companyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CompanyId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CategoryId { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("responsibilities")]
    public List<string> Responsibilities { get; set; } = new();

    [BsonElement("mustHaveSkills")]
    public List<string> MustHaveSkills { get; set; } = new();

    [BsonElement("niceToHaveSkills")]
    public List<string> NiceToHaveSkills { get; set; } = new();

    [BsonElement("minExperienceYears")]
    public int MinExperienceYears { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
