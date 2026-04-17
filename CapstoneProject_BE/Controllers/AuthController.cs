using Microsoft.AspNetCore.Mvc;
using Repositories;
using Services;
using Services.DTOs;
using Microsoft.AspNetCore.RateLimiting;

namespace capstone_be.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IAccessLogService _accessLogService;
    private readonly ICaptchaService _captchaService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IWebHostEnvironment env, IAccessLogService accessLogService, ICaptchaService captchaService)
    {
        _authService = authService;
        _logger = logger;
        _env = env;
        _accessLogService = accessLogService;
        _captchaService = captchaService;
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].ToString();
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    [HttpPost("login")]
    [EnableRateLimiting("Strict")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { message = "Request body is null" });
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                Console.WriteLine($"Login failed. IdToken is empty. Campus: {request.Campus}");
                return BadRequest(new { message = "IdToken is required" });
            }

            var captchaToken = Request.Headers["X-Captcha-Token"].ToString();
            if (!await _captchaService.VerifyCaptchaAsync(captchaToken))
            {
                return BadRequest(new { message = "Captcha validation failed" });
            }

            var result = await _authService.GoogleLoginAsync(request);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
            return Ok(new LoginResponseDTO
            {
                AccessToken = result.AccessToken,
                Token = result.AccessToken,
                UserInfo = result.UserInfo
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var ipAddress = GetIpAddress();
            await _accessLogService.CreateLogAsync(new BusinessObjects.Models.AccessLog
            {
                UserId = null,
                UserEmail = request?.Campus ?? "Unknown", // Assuming campus as fallback
                IpAddress = ipAddress,
                Action = "Login Failed",
                IsSuccess = false,
                Description = ex.Message
            });
            _logger.LogWarning(ex, "Unauthorized login attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Configuration error during login");
            return StatusCode(500, new { message = "Server configuration error" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return StatusCode(500, new { message = "Lỗi hệ thống trong quá trình đăng nhập. Vui lòng liên hệ quản trị viên." });
        }
    }

    [HttpPost("login/credentials")]
    [EnableRateLimiting("Strict")]
    public async Task<IActionResult> LoginWithCredentials([FromBody] CredentialLoginRequestDTO request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { message = "Request body is null" });

            var captchaToken = Request.Headers["X-Captcha-Token"].ToString();
            if (!await _captchaService.VerifyCaptchaAsync(captchaToken))
            {
                return BadRequest(new { message = "Captcha validation failed" });
            }

            var result = await _authService.CredentialLoginAsync(request);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
            return Ok(new LoginResponseDTO
            {
                AccessToken = result.AccessToken,
                Token = result.AccessToken,
                UserInfo = result.UserInfo
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var ipAddress = GetIpAddress();
            await _accessLogService.CreateLogAsync(new BusinessObjects.Models.AccessLog
            {
                UserId = null,
                UserEmail = request?.Username ?? "Unknown",
                IpAddress = ipAddress,
                Action = "Login Failed (Credentials)",
                IsSuccess = false,
                Description = ex.Message
            });
            _logger.LogWarning(ex, "Unauthorized credential login attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during credential login");
            return StatusCode(500, new { message = "Lỗi hệ thống trong quá trình đăng nhập. Vui lòng liên hệ quản trị viên." });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        var result = await _authService.RefreshTokenAsync(refreshToken);
        if (result == null)
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(new RefreshResponseDTO { AccessToken = result.AccessToken });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        await _authService.RevokeRefreshTokenAsync(refreshToken);
        Response.Cookies.Delete(RefreshTokenCookieName);

        // Log logout using claims
        var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        var ipAddress = GetIpAddress();
        int? userId = null;
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
        {
            userId = parsedId;
        }

        await _accessLogService.CreateLogAsync(new BusinessObjects.Models.AccessLog
        {
            UserId = userId,
            UserEmail = !string.IsNullOrEmpty(userEmail) ? userEmail : "Unknown (Logout)",
            IpAddress = ipAddress,
            Action = "Logout",
            IsSuccess = true,
            Description = "User logged out successfully"
        });

        return Ok(new { message = "Logged out" });
    }

    private void SetRefreshTokenCookie(string? refreshToken, DateTime? expiresAt)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            // Allow cross-site cookie sending for refresh calls (FE x BE different origins).
            // In dev we keep Lax to avoid Secure=false issues.
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/",
            MaxAge = expiresAt.HasValue ? (TimeSpan)(expiresAt.Value - DateTime.UtcNow) : TimeSpan.FromDays(7)
        };
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, options);
    }
}
