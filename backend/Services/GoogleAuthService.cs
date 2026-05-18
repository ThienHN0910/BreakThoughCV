using BreakThroughCV.API.Settings;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace BreakThroughCV.API.Services;

public class GoogleAuthService
{
    private readonly GoogleAuthSettings _settings;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(IOptions<GoogleAuthSettings> settings, ILogger<GoogleAuthService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<GoogleJsonWebSignature.Payload?> ValidateTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _settings.ClientId }
            };
            // If ClientId is not set, skip audience validation (for dev)
            if (string.IsNullOrEmpty(_settings.ClientId))
            {
                settings = new GoogleJsonWebSignature.ValidationSettings();
            }
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Invalid Google JWT: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return null;
        }
    }
}
