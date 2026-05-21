using System.Text;
using System.Text.Json;
using BreakThroughCV.API.DTOs;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Settings;
using Microsoft.Extensions.Options;

namespace BreakThroughCV.API.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IOptions<GeminiSettings> settings, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    private async Task<string?> CallGeminiAsync(string prompt)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent";
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _settings.ApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error {StatusCode}: {Error}", response.StatusCode, error);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Gemini API");
            return null;
        }
    }

    public async Task<List<JobSuggestionResult>?> SuggestJobsAsync(string cvText, List<Job> jobs)
    {
        var jobsJson = JsonSerializer.Serialize(jobs.Select(j => new
        {
            jobId = j.Id,
            title = j.Title,
            description = j.Description,
            mustHaveSkills = j.MustHaveSkills,
            niceToHaveSkills = j.NiceToHaveSkills
        }));

        var prompt = $$"""
Bạn là một AI trợ lý hướng nghiệp. Hãy phân tích CV của người dùng và chọn ra top 3 công việc phù hợp nhất trong danh sách Jobs được cung cấp.
Trả về định dạng JSON gồm danh sách JobId kèm lý do ngắn gọn vì sao phù hợp.

CV của ứng viên:
{{cvText}}

Danh sách Jobs:
{{jobsJson}}

Trả về JSON theo format: { "suggestions": [ { "jobId": "...", "reason": "..." }, ... ] }
""";

        var result = await CallGeminiAsync(prompt);
        if (result == null) return null;

        try
        {
            using var doc = JsonDocument.Parse(result);
            var suggestions = doc.RootElement.GetProperty("suggestions");
            return JsonSerializer.Deserialize<List<JobSuggestionResult>>(suggestions.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini job suggestion response: {Result}", result);
            return null;
        }
    }

    public async Task<CvReviewResponse?> ReviewCvAsync(string cvText, Job job)
    {
        var jobJson = JsonSerializer.Serialize(new
        {
            title = job.Title,
            description = job.Description,
            responsibilities = job.Responsibilities,
            mustHaveSkills = job.MustHaveSkills,
            niceToHaveSkills = job.NiceToHaveSkills
        });

        var prompt = $$"""
Bạn là Chuyên gia nhân sự và Hệ thống lọc ATS. Hãy chấm điểm CV này dựa trên Job Description (JD) được cung cấp theo thang điểm 100.
Hãy chỉ ra các từ khóa (Keywords) quan trọng trong JD mà CV đang thiếu.
Gợi ý chi tiết cách viết lại đoạn văn kinh nghiệm làm việc cũ trong CV để khớp với tiêu chí của JD nhằm đạt tỷ lệ trúng tuyển cao nhất.

CV của ứng viên:
{{cvText}}

Job Description:
{{jobJson}}

Trả về JSON theo format nghiêm ngặt:
{
  "score": <number 0-100>,
  "missing_keywords": ["keyword1", "keyword2", ...],
  "critical_fixes": ["fix1", "fix2", ...],
  "tailored_suggestions": [
    {
      "section": "<tên section trong CV>",
      "original_text": "<đoạn văn gốc>",
      "suggested_text": "<đoạn văn được cải thiện>"
    }
  ]
}
""";

        var result = await CallGeminiAsync(prompt);
        if (result == null) return null;

        try
        {
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            var missingKeywords = root.GetProperty("missing_keywords")
                .EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            var criticalFixes = root.GetProperty("critical_fixes")
                .EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            var tailoredSuggestions = root.GetProperty("tailored_suggestions")
                .EnumerateArray()
                .Select(x => new TailoredSuggestionDto(
                    x.GetProperty("section").GetString() ?? "",
                    x.GetProperty("original_text").GetString() ?? "",
                    x.GetProperty("suggested_text").GetString() ?? ""
                )).ToList();

            return new CvReviewResponse(
                Id: string.Empty,
                Score: root.GetProperty("score").GetInt32(),
                MissingKeywords: missingKeywords,
                CriticalFixes: criticalFixes,
                TailoredSuggestions: tailoredSuggestions,
                CreatedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini CV review response: {Result}", result);
            return null;
        }
    }

    private static List<string> BuildJobKeywordFallback(string? title, string? categoryName, string? description, string? targetField)
    {
        var input = $"{title} {categoryName} {description}".ToLowerInvariant();
        var keywords = new List<string>();

        void Add(params string[] values)
        {
            foreach (var value in values)
            {
                if (!keywords.Contains(value, StringComparer.OrdinalIgnoreCase))
                    keywords.Add(value);
            }
        }

        if (targetField == "responsibilities")
        {
            if (input.Contains("frontend") || input.Contains("front-end") || input.Contains("vue") || input.Contains("react"))
                Add("Xây dựng giao diện responsive", "Tích hợp REST API", "Tối ưu trải nghiệm người dùng", "Phối hợp với backend");
            else if (input.Contains("backend") || input.Contains("back-end") || input.Contains(".net") || input.Contains("api"))
                Add("Thiết kế REST API", "Xử lý logic nghiệp vụ", "Tối ưu truy vấn dữ liệu", "Viết unit test");
            else
                Add("Phân tích yêu cầu", "Phát triển tính năng", "Kiểm thử và sửa lỗi", "Viết tài liệu kỹ thuật");

            return keywords.Take(12).ToList();
        }

        if (input.Contains("frontend") || input.Contains("front-end") || input.Contains("vue") || input.Contains("react"))
            Add("HTML", "CSS", "JavaScript", "TypeScript", "Vue.js", "React", "Responsive UI", "REST API");

        if (input.Contains("backend") || input.Contains("back-end") || input.Contains(".net") || input.Contains("api"))
            Add("C#", ".NET", "ASP.NET Core", "REST API", "SQL", "MongoDB", "JWT", "Unit Testing");

        if (input.Contains("mobile") || input.Contains("android") || input.Contains("ios") || input.Contains("flutter"))
            Add("Flutter", "React Native", "Android", "iOS", "Dart", "Mobile UI", "Firebase", "API Integration");

        if (input.Contains("data") || input.Contains("ai") || input.Contains("machine") || input.Contains("python"))
            Add("Python", "SQL", "Machine Learning", "Data Analysis", "Pandas", "TensorFlow", "ETL", "Visualization");

        if (input.Contains("devops") || input.Contains("cloud") || input.Contains("aws") || input.Contains("docker"))
            Add("Docker", "CI/CD", "Linux", "AWS", "Azure", "Kubernetes", "Monitoring", "Git");

        if (keywords.Count == 0)
            Add("Git", "Problem Solving", "REST API", "Database", "Testing", "Agile", "Documentation", "Communication");

        return keywords.Take(12).ToList();
    }

    public async Task<List<string>?> SuggestJobKeywordsAsync(string? title, string? categoryName, string? description, string? targetField)
    {
        var targetInstruction = targetField switch
        {
            "responsibilities" => "Đề xuất các trách nhiệm công việc ngắn gọn, dạng cụm hành động.",
            "niceToHaveSkills" => "Đề xuất các kỹ năng nice-to-have, công nghệ bổ sung hoặc lợi thế.",
            _ => "Đề xuất các kỹ năng must-have quan trọng nhất."
        };

        var prompt = $$"""
Bạn là trợ lý tuyển dụng IT. Dựa trên tiêu đề công việc, chuyên ngành/danh mục và mô tả JD, hãy đề xuất 8 đến 12 mục phù hợp cho ô đang được recruiter chọn.
{{targetInstruction}}
Ưu tiên nội dung ngắn, thực tế, không đưa câu quá dài.

Tiêu đề:
{{title}}

Chuyên ngành/danh mục:
{{categoryName}}

Mô tả:
{{description}}

Trả về JSON đúng format: { "keywords": ["item1", "item2", "..."] }
""";

        var result = await CallGeminiAsync(prompt);
        if (result == null) return BuildJobKeywordFallback(title, categoryName, description, targetField);

        try
        {
            using var doc = JsonDocument.Parse(result);
            return doc.RootElement
                .GetProperty("keywords")
                .EnumerateArray()
                .Select(x => x.GetString()?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini job keyword suggestion response: {Result}", result);
            return BuildJobKeywordFallback(title, categoryName, description, targetField);
        }
    }
}
