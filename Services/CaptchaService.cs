using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Services
{
    public class CaptchaService : ICaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly ISystemParameterService _systemParameterService;
        private readonly ILogger<CaptchaService> _logger;

        public CaptchaService(HttpClient httpClient, ISystemParameterService systemParameterService, ILogger<CaptchaService> logger)
        {
            _httpClient = httpClient;
            _systemParameterService = systemParameterService;
            _logger = logger;
        }

        public async Task<bool> VerifyCaptchaAsync(string captchaToken)
        {
            if (string.IsNullOrWhiteSpace(captchaToken))
                return false;

            try
            {
                var secretKey = await _systemParameterService.GetParameterByKeyAsync("CAPTCHA_SECRET_KEY");
                if (secretKey == null || string.IsNullOrWhiteSpace(secretKey.Value))
                {
                    _logger.LogWarning("CAPTCHA_SECRET_KEY is not configured.");
                    return true; // Optionally fail open if not configured
                }

                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", 
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("secret", secretKey.Value),
                        new KeyValuePair<string, string>("response", captchaToken)
                    }));

                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseString);

                if (result.TryGetProperty("success", out var successElement))
                {
                    return successElement.GetBoolean();
                }

                return false;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error verifying captcha.");
                return false;
            }
        }
    }
}
