using System.Text.Json;
using System.Text.Json.Serialization;

namespace BreakThroughCV.API.DTOs;

public record CreateAiAccessPaymentResponse(
    bool AlreadyPaid,
    bool AiAccessEnabled,
    long? OrderCode,
    string? PaymentLinkId,
    string? CheckoutUrl
);

public record CreateAiAccessPaymentRequest(
    string Plan
);

public record AiAccessPlanDto(
    string Key,
    string Label,
    int Amount,
    int Days
);

public record VerifyAiAccessPaymentResponse(
    bool AiAccessEnabled,
    string Status
);

public record AiAccessPurchaseDto(
    long OrderCode,
    string Plan,
    int Amount,
    string Status,
    string? Description,
    string? PaymentLinkId,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? AccessFromAt,
    DateTime? AccessToAt
);

public sealed class PayOsWebhookRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}
