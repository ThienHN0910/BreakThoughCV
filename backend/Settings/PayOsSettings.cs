namespace BreakThroughCV.API.Settings;

public class PayOsSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";

    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;

    // Legacy single-plan fields (kept for backward compatibility)
    public int AiAccessAmount { get; set; } = 1000;
    public string AiAccessDescription { get; set; } = "AI_ACCESS";

    // Multi-plan pricing (defaults per requirement)
    public int AiAccessWeekAmount { get; set; } = 2000;
    public int AiAccessMonthAmount { get; set; } = 20000;
    public int AiAccessYearAmount { get; set; } = 100000;

    // Durations
    public int AiAccessWeekDays { get; set; } = 7;
    public int AiAccessMonthDays { get; set; } = 30;
    public int AiAccessYearDays { get; set; } = 365;
}
