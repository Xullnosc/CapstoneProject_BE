using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services;

namespace capstone_be.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request body is null" });
                }
                
                if (string.IsNullOrWhiteSpace(request.IdToken))
                {
                    // Debugging: Log what we received
                    Console.WriteLine($"Login failed. IdToken is empty. Campus: {request.Campus}");
                    return BadRequest(new { message = "IdToken is required" });
                }

                var response = await _authService.GoogleLoginAsync(request);
                return Ok(response);
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
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }
    }
}
