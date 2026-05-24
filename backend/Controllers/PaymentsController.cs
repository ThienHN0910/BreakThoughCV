using System.Security.Claims;
using System.Text.Json;
using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using BreakThroughCV.API.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BreakThroughCV.API.Controllers;

[ApiController]
[Route("api/payments/payos")]
public class PaymentsController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly PayOsService _payOs;
    private readonly PayOsSettings _settings;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(MongoDbService db, PayOsService payOs, IOptions<PayOsSettings> settings, ILogger<PaymentsController> logger)
    {
        _db = db;
        _payOs = payOs;
        _settings = settings.Value;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private static bool IsAiAccessActive(User user, DateTime now)
    {
        if (user.AiAccessPaidAt == null) return false;
        if (user.AiAccessExpiresAt == null) return true; // legacy: lifetime access
        return user.AiAccessExpiresAt > now;
    }

    private (string Plan, int Amount, int Days, string Description) ResolveAiPlan(string? raw)
    {
        var plan = (raw ?? string.Empty).Trim().ToUpperInvariant();
        if (plan is not ("WEEK" or "MONTH" or "YEAR")) plan = "MONTH";

        return plan switch
        {
            "WEEK" => ("WEEK", _settings.AiAccessWeekAmount, _settings.AiAccessWeekDays, "AI_WEEK"),
            "YEAR" => ("YEAR", _settings.AiAccessYearAmount, _settings.AiAccessYearDays, "AI_YEAR"),
            _ => ("MONTH", _settings.AiAccessMonthAmount, _settings.AiAccessMonthDays, "AI_MONTH")
        };
    }

    private int ResolveAiPlanDays(string? plan)
    {
        var p = (plan ?? string.Empty).Trim().ToUpperInvariant();
        return p switch
        {
            "WEEK" => _settings.AiAccessWeekDays,
            "YEAR" => _settings.AiAccessYearDays,
            _ => _settings.AiAccessMonthDays
        };
    }

    [HttpPost("ai-access/create")]
    [Authorize]
    public async Task<IActionResult> CreateAiAccessPayment([FromBody] CreateAiAccessPaymentRequest? request)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

        var now = DateTime.UtcNow;
        if (IsAiAccessActive(user, now))
        {
            return Ok(new CreateAiAccessPaymentResponse(
                AlreadyPaid: true,
                AiAccessEnabled: true,
                OrderCode: null,
                PaymentLinkId: null,
                CheckoutUrl: null
            ));
        }

        if (!_payOs.IsConfigured())
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(_settings.ClientId)) missing.Add("PayOsSettings:ClientId");
            if (string.IsNullOrWhiteSpace(_settings.ApiKey)) missing.Add("PayOsSettings:ApiKey");
            if (string.IsNullOrWhiteSpace(_settings.ChecksumKey)) missing.Add("PayOsSettings:ChecksumKey");
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl)) missing.Add("PayOsSettings:BaseUrl");

            return StatusCode(500, new
            {
                message = "PayOS is not configured",
                missing,
                hint = "Fill PayOsSettings in appsettings.json (or set env vars like PayOsSettings__ClientId, PayOsSettings__ApiKey, PayOsSettings__ChecksumKey)."
            });
        }

        if (string.IsNullOrWhiteSpace(_settings.ReturnUrl) || string.IsNullOrWhiteSpace(_settings.CancelUrl))
            return StatusCode(500, new { message = "PayOS ReturnUrl/CancelUrl is not configured" });

        var resolved = ResolveAiPlan(request?.Plan);
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var amount = resolved.Amount;
        var description = resolved.Description;

        if (amount <= 0) return BadRequest(new { message = "Invalid amount" });

        // payOS description can be constrained; keep short and stable
        if (string.IsNullOrWhiteSpace(description)) description = "AI_ACCESS";

        var data = await _payOs.CreatePaymentLinkAsync(orderCode, amount, description, _settings.ReturnUrl, _settings.CancelUrl);
        if (data == null)
            return StatusCode(503, new { message = "Unable to create PayOS payment link" });

        var tx = new PaymentTransaction
        {
            UserId = userId,
            Type = "AI_ACCESS",
            Plan = resolved.Plan,
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            PaymentLinkId = data.PaymentLinkId,
            Status = data.Status ?? "PENDING",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _db.PaymentTransactions.InsertOneAsync(tx);

        return Ok(new CreateAiAccessPaymentResponse(
            AlreadyPaid: false,
            AiAccessEnabled: false,
            OrderCode: orderCode,
            PaymentLinkId: data.PaymentLinkId,
            CheckoutUrl: data.CheckoutUrl
        ));
    }

    [HttpGet("ai-access/plans")]
    [Authorize]
    public IActionResult GetAiAccessPlans()
    {
        var plans = new List<AiAccessPlanDto>
        {
            new(
                Key: "WEEK",
                Label: "1 tuần",
                Amount: _settings.AiAccessWeekAmount,
                Days: _settings.AiAccessWeekDays
            ),
            new(
                Key: "MONTH",
                Label: "1 tháng",
                Amount: _settings.AiAccessMonthAmount,
                Days: _settings.AiAccessMonthDays
            ),
            new(
                Key: "YEAR",
                Label: "1 năm",
                Amount: _settings.AiAccessYearAmount,
                Days: _settings.AiAccessYearDays
            )
        };

        return Ok(plans);
    }

    [HttpGet("ai-access/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyAiAccessPayment([FromQuery] long orderCode)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

        var now = DateTime.UtcNow;
        if (IsAiAccessActive(user, now))
        {
            return Ok(new VerifyAiAccessPaymentResponse(AiAccessEnabled: true, Status: "PAID"));
        }

        // Ensure the order code belongs to this user (basic protection)
        var tx = await _db.PaymentTransactions.Find(t => t.OrderCode == orderCode && t.UserId == userId && t.Type == "AI_ACCESS")
            .FirstOrDefaultAsync();
        if (tx == null)
            return NotFound(new { message = "Payment transaction not found" });

        var info = await _payOs.GetPaymentLinkInfoAsync(orderCode.ToString());
        if (info == null)
            return StatusCode(503, new { message = "Unable to verify PayOS payment" });

        var status = info.Status ?? "PENDING";

        var updates = Builders<PaymentTransaction>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, now);

        if (status == "PAID" && tx.AccessToAt == null)
        {
            var days = ResolveAiPlanDays(tx.Plan);
            var baseTime = user.AiAccessExpiresAt.HasValue && user.AiAccessExpiresAt.Value > now
                ? user.AiAccessExpiresAt.Value
                : now;
            var expiresAt = baseTime.AddDays(days);

            updates = updates
                .Set(x => x.PaidAt, now)
                .Set(x => x.AccessFromAt, baseTime)
                .Set(x => x.AccessToAt, expiresAt);

            var userUpdate = Builders<User>.Update
                .Set(u => u.AiAccessPaidAt, now)
                .Set(u => u.AiAccessExpiresAt, expiresAt);
            await _db.Users.UpdateOneAsync(u => u.Id == userId, userUpdate);
        }

        await _db.PaymentTransactions.UpdateOneAsync(t => t.Id == tx.Id, updates);

        return Ok(new VerifyAiAccessPaymentResponse(
            AiAccessEnabled: status == "PAID",
            Status: status
        ));
    }

    [HttpGet("ai-access/history")]
    [Authorize]
    public async Task<IActionResult> GetAiAccessHistory()
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var items = await _db.PaymentTransactions
            .Find(t => t.UserId == userId && t.Type == "AI_ACCESS")
            .SortByDescending(t => t.CreatedAt)
            .Limit(100)
            .ToListAsync();

        var dto = items.Select(t => new AiAccessPurchaseDto(
            OrderCode: t.OrderCode,
            Plan: string.IsNullOrWhiteSpace(t.Plan) ? "MONTH" : t.Plan,
            Amount: t.Amount,
            Status: t.Status,
            Description: t.Description,
            PaymentLinkId: t.PaymentLinkId,
            CreatedAt: t.CreatedAt,
            PaidAt: t.PaidAt,
            AccessFromAt: t.AccessFromAt,
            AccessToAt: t.AccessToAt
        ));

        return Ok(dto);
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOsWebhook([FromBody] PayOsWebhookRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.ChecksumKey))
                return Ok(new { received = true });

            var computed = PayOsService.CreateWebhookSignature(request.Data, _settings.ChecksumKey);
            if (!computed.Equals(request.Signature ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid PayOS webhook signature");
                return BadRequest(new { message = "Invalid signature" });
            }

            if (request.Data.ValueKind != JsonValueKind.Object)
                return Ok(new { received = true });

            if (!request.Data.TryGetProperty("orderCode", out var orderCodeEl))
                return Ok(new { received = true });

            if (!orderCodeEl.TryGetInt64(out var orderCode))
                return Ok(new { received = true });

            var tx = await _db.PaymentTransactions.Find(t => t.OrderCode == orderCode && t.Type == "AI_ACCESS").FirstOrDefaultAsync();
            if (tx == null)
                return Ok(new { received = true });

            // payOS payment webhook indicates success; still double-check its nested code when available.
            var paid = request.Success;
            if (request.Data.TryGetProperty("code", out var codeEl))
            {
                paid = paid && string.Equals(codeEl.GetString(), "00", StringComparison.OrdinalIgnoreCase);
            }

            var updates = Builders<PaymentTransaction>.Update
                .Set(t => t.UpdatedAt, DateTime.UtcNow)
                .Set(t => t.RawWebhook, JsonSerializer.Serialize(request))
                .Set(t => t.Status, paid ? "PAID" : tx.Status);

            if (paid && tx.AccessToAt == null)
            {
                var now = DateTime.UtcNow;
                var user = await _db.Users.Find(u => u.Id == tx.UserId).FirstOrDefaultAsync();
                if (user != null)
                {
                    var days = ResolveAiPlanDays(tx.Plan);
                    var baseTime = user.AiAccessExpiresAt.HasValue && user.AiAccessExpiresAt.Value > now
                        ? user.AiAccessExpiresAt.Value
                        : now;
                    var expiresAt = baseTime.AddDays(days);

                    updates = updates
                        .Set(t => t.PaidAt, now)
                        .Set(t => t.AccessFromAt, baseTime)
                        .Set(t => t.AccessToAt, expiresAt);

                    await _db.Users.UpdateOneAsync(
                        u => u.Id == tx.UserId,
                        Builders<User>.Update
                            .Set(u => u.AiAccessPaidAt, now)
                            .Set(u => u.AiAccessExpiresAt, expiresAt)
                    );
                }
            }

            await _db.PaymentTransactions.UpdateOneAsync(t => t.Id == tx.Id, updates);

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayOS webhook");
            // Return 2xx to avoid repeated retries causing noise.
            return Ok(new { received = true });
        }
    }
}
