using BreakThroughCV.API.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace BreakThroughCV.API.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IOptions<CloudinarySettings> settings, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        var account = new Account(
            settings.Value.CloudName,
            settings.Value.ApiKey,
            settings.Value.ApiSecret
        );
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
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
}
