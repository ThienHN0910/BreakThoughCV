using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
using BreakThroughCV.API.Settings;
using Microsoft.Extensions.Options;

namespace BreakThroughCV.API.Services;

public class PayOsService
{
    private readonly HttpClient _httpClient;
    private readonly PayOsSettings _settings;
    private readonly ILogger<PayOsService> _logger;

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PayOsService(HttpClient httpClient, IOptions<PayOsSettings> settings, ILogger<PayOsService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_settings.ClientId)
           && !string.IsNullOrWhiteSpace(_settings.ApiKey)
           && !string.IsNullOrWhiteSpace(_settings.ChecksumKey)
           && !string.IsNullOrWhiteSpace(_settings.BaseUrl);

    public async Task<PayOsPaymentRequestData?> CreatePaymentLinkAsync(long orderCode, int amount, string description, string returnUrl, string cancelUrl)
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("PayOS is not configured");
            return null;
        }

        var signature = CreatePaymentRequestSignature(orderCode, amount, description, returnUrl, cancelUrl, _settings.ChecksumKey);

        var payload = new PayOsCreatePaymentRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            ReturnUrl = returnUrl,
            CancelUrl = cancelUrl,
            Signature = signature,
            Items = new List<PayOsItem>
            {
                new()
                {
                    Name = "AI Access",
                    Quantity = 1,
                    Price = amount
                }
            }
        };

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v2/payment-requests";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("x-client-id", _settings.ClientId);
        req.Headers.Add("x-api-key", _settings.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload, JsonWriteOptions), Encoding.UTF8, "application/json");

        using var resp = await _httpClient.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("PayOS create payment link failed {StatusCode}: {Body}", resp.StatusCode, body);
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<PayOsApiResponse<PayOsPaymentRequestData>>(body, JsonReadOptions);
            if (parsed?.Code != "00")
            {
                _logger.LogError("PayOS create payment link error {Code}: {Desc}", parsed?.Code, parsed?.Desc);
                return null;
            }

            return parsed.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse PayOS create payment link response: {Body}", body);
            return null;
        }
    }

    public async Task<PayOsPaymentLinkInfoData?> GetPaymentLinkInfoAsync(string idOrOrderCode)
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("PayOS is not configured");
            return null;
        }

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v2/payment-requests/{idOrOrderCode}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("x-client-id", _settings.ClientId);
        req.Headers.Add("x-api-key", _settings.ApiKey);

        using var resp = await _httpClient.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("PayOS get payment link info failed {StatusCode}: {Body}", resp.StatusCode, body);
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<PayOsApiResponse<PayOsPaymentLinkInfoData>>(body, JsonReadOptions);
            if (parsed?.Code != "00")
            {
                _logger.LogError("PayOS get payment link info error {Code}: {Desc}", parsed?.Code, parsed?.Desc);
                return null;
            }

            return parsed.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse PayOS get payment link info response: {Body}", body);
            return null;
        }
    }

    public static string CreateWebhookSignature(JsonElement dataObject, string checksumKey)
    {
        if (dataObject.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Webhook data must be a JSON object", nameof(dataObject));

        var props = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var prop in dataObject.EnumerateObject())
        {
            props[prop.Name] = NormalizeJsonValue(prop.Value);
        }

        var raw = string.Join("&", props.Select(kv => $"{kv.Key}={kv.Value}"));
        return HmacSha256(raw, checksumKey);
    }

    private static string CreatePaymentRequestSignature(long orderCode, int amount, string description, string returnUrl, string cancelUrl, string checksumKey)
    {
        // Must be sorted by alphabet:
        // amount, cancelUrl, description, orderCode, returnUrl
        var raw = $"amount={amount.ToString(CultureInfo.InvariantCulture)}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode.ToString(CultureInfo.InvariantCulture)}&returnUrl={returnUrl}";
        return HmacSha256(raw, checksumKey);
    }

    private static string HmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => NormalizeString(value.GetString()),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            JsonValueKind.Object => NormalizeComplexJson(value),
            JsonValueKind.Array => NormalizeComplexJson(value),
            _ => value.GetRawText()
        };
    }

    private static string NormalizeString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        if (string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        return value;
    }

    private static string NormalizeComplexJson(JsonElement element)
    {
        // For objects/arrays: deep-sort object keys (arrays keep order), then JSON stringify.
        // This matches payOS docs guidance for signature verification.
        var node = JsonNode.Parse(element.GetRawText());
        var sorted = DeepSort(node);
        return sorted?.ToJsonString(JsonWriteOptions) ?? string.Empty;
    }

    private static JsonNode? DeepSort(JsonNode? node)
    {
        return node switch
        {
            JsonObject obj => DeepSortObject(obj),
            JsonArray arr => DeepSortArray(arr),
            _ => node
        };
    }

    private static JsonNode DeepSortObject(JsonObject obj)
    {
        var next = new JsonObject();
        foreach (var kv in obj.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            next[kv.Key] = DeepSort(kv.Value);
        }
        return next;
    }

    private static JsonNode DeepSortArray(JsonArray arr)
    {
        var next = new JsonArray();
        foreach (var el in arr)
        {
            next.Add(DeepSort(el));
        }
        return next;
    }

    private sealed class PayOsCreatePaymentRequest
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public List<PayOsItem>? Items { get; set; }
    }

    private sealed class PayOsItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Price { get; set; }
    }

    private sealed class PayOsApiResponse<T>
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public sealed class PayOsPaymentRequestData
    {
        public long OrderCode { get; set; }
        public string PaymentLinkId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    public sealed class PayOsPaymentLinkInfoData
    {
        public string Id { get; set; } = string.Empty;
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public int AmountPaid { get; set; }
        public int AmountRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
    }
}
