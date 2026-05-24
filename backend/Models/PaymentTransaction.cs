using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BreakThroughCV.API.Models;

public class PaymentTransaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = "AI_ACCESS";

    [BsonElement("orderCode")]
    public long OrderCode { get; set; }

    [BsonElement("amount")]
    public int Amount { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("paymentLinkId")]
    public string? PaymentLinkId { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "PENDING";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("rawWebhook")]
    public string? RawWebhook { get; set; }
}
