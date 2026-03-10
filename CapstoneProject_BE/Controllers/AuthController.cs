using Microsoft.AspNetCore.Mvc;
using Services;
using Services.DTOs;

namespace capstone_be.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IWebHostEnvironment env)
    {
        _authService = authService;
        _logger = logger;
        _env = env;
    }

    [HttpPost("login")]
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
    public async Task<IActionResult> LoginWithCredentials([FromBody] CredentialLoginRequestDTO request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { message = "Request body is null" });

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
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = expiresAt.HasValue ? (TimeSpan)(expiresAt.Value - DateTime.UtcNow) : TimeSpan.FromDays(7)
        };
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, options);
    }
}
