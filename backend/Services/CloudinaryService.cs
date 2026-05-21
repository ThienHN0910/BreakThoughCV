using BreakThroughCV.API.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace BreakThroughCV.API.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public CloudinaryService(IOptions<CloudinarySettings> settings, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        _cloudName = settings.Value.CloudName;
        _apiKey = settings.Value.ApiKey;
        _apiSecret = settings.Value.ApiSecret;
        var account = new Account(_cloudName, _apiKey, _apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<Stream?> DownloadFileAsync(string fileUrl)
    {
        try
        {
            // Try to derive public id from delivery URL
            var uri = new Uri(fileUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

            // find 'upload' segment
            var uploadIndex = segments.FindIndex(s => s.Equals("upload", StringComparison.OrdinalIgnoreCase));
            if (uploadIndex < 0 || uploadIndex + 1 >= segments.Count) return null;

            var afterUpload = segments.Skip(uploadIndex + 1).ToList();
            // remove version if present (v123)
            if (afterUpload.Count > 0 && afterUpload[0].Length > 0 && afterUpload[0][0] == 'v' && afterUpload[0].Substring(1).All(char.IsDigit))
                afterUpload.RemoveAt(0);

            if (afterUpload.Count == 0) return null;

            // join remaining segments and strip file extension
            var last = afterUpload.Last();
            var dot = last.LastIndexOf('.');
            if (dot >= 0) afterUpload[afterUpload.Count - 1] = last.Substring(0, dot);
            var publicId = string.Join('/', afterUpload);

            // Call Admin API to get resource metadata
            var adminUrl = $"https://api.cloudinary.com/v1_1/{_cloudName}/resources/raw/upload/{Uri.EscapeDataString(publicId)}";
            using var client = new HttpClient();
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_apiKey}:{_apiSecret}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            var metaResp = await client.GetAsync(adminUrl);
            if (!metaResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Cloudinary admin API returned {Status} when fetching metadata for {PublicId}", metaResp.StatusCode, publicId);
                return null;
            }

            var metaJson = await metaResp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(metaJson);
            if (!doc.RootElement.TryGetProperty("secure_url", out var secureUrlEl))
                return null;

            var secureUrl = secureUrlEl.GetString();
            if (string.IsNullOrEmpty(secureUrl)) return null;

            // Fetch the actual file bytes from secure_url
            using var fileResp = await client.GetAsync(secureUrl);
            if (!fileResp.IsSuccessStatusCode) return null;

            var ms = new MemoryStream();
            await fileResp.Content.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from Cloudinary API for URL {Url}", fileUrl);
            return null;
        }
    }

    public async Task<bool> DeleteFileByUrlAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
            var uploadIndex = segments.FindIndex(s => s.Equals("upload", StringComparison.OrdinalIgnoreCase));
            if (uploadIndex < 0 || uploadIndex + 1 >= segments.Count) return false;
            var afterUpload = segments.Skip(uploadIndex + 1).ToList();
            if (afterUpload.Count > 0 && afterUpload[0].Length > 0 && afterUpload[0][0] == 'v' && afterUpload[0].Substring(1).All(char.IsDigit))
                afterUpload.RemoveAt(0);
            if (afterUpload.Count == 0) return false;
            var last = afterUpload.Last();
            var dot = last.LastIndexOf('.');
            if (dot >= 0) afterUpload[afterUpload.Count - 1] = last.Substring(0, dot);
            var publicId = string.Join('/', afterUpload);

            var deleteUrl = $"https://api.cloudinary.com/v1_1/{_cloudName}/resources/raw/upload/{Uri.EscapeDataString(publicId)}";
            using var client = new HttpClient();
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_apiKey}:{_apiSecret}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            var resp = await client.DeleteAsync(deleteUrl);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Cloudinary file for URL {Url}", fileUrl);
            return false;
        }
    }

    public async Task<string?> UploadImageAsync(IFormFile file, string folder = "avatars")
    {
        try
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", result.Error.Message);
                return null;
            }
            return result.SecureUrl?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image to Cloudinary");
            return null;
        }
    }

    public async Task<string?> UploadFileAsync(IFormFile file, string folder = "cvs")
    {
        try
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", result.Error.Message);
                return null;
            }
            return result.SecureUrl?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Cloudinary");
            return null;
        }
    }

    public async Task<string?> UploadFileStreamAsync(Stream stream, string fileName, string folder = "cvs")
    {
        try
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", result.Error.Message);
                return null;
            }
            return result.SecureUrl?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload stream to Cloudinary");
            return null;
        }
    }
}
