using Services.DTOs;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UserController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized();
                var userId = int.Parse(userIdClaim.Value);

                var profile = await _userService.GetProfileAsync(userId);
                if (profile == null) return NotFound(new { message = "User not found" });

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred.", details = "An error occurred while getting the profile." });
            }
        }

        [HttpGet("search")]
        [Authorize(Roles = CampusConstants.Roles.Student + "," + CampusConstants.Roles.Lecturer + "," + CampusConstants.Roles.HOD)]
        public async Task<IActionResult> SearchStudents([FromQuery] string term, [FromQuery] int? teamId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return BadRequest(new { message = "Search term cannot be empty" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized();
                var currentUserId = int.Parse(userIdClaim.Value);

                var students = await _userService.SearchStudentsAsync(term, currentUserId, teamId);
                return Ok(students);
            }
            catch (Exception ex)
            {
                 return StatusCode(500, new { message = "An internal server error occurred.", details = ex.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO profileDto)
        {
            try
            {
                var userIdClaim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized();
                var userId = int.Parse(userIdClaim.Value);

                var updatedUser = await _userService.UpdateProfileAsync(userId, profileDto);
                if (updatedUser == null) return NotFound(new { message = "User not found" });

                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred.", details = "An error occurred while updating the profile." });
            }
        }

        [HttpPut("profile/password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDTO passwordDto)
        {
            try
            {
                if (passwordDto == null || string.IsNullOrWhiteSpace(passwordDto.NewPassword))
                    return BadRequest(new { message = "Mật khẩu mới là bắt buộc." });

                var userIdClaim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized();
                var userId = int.Parse(userIdClaim.Value);

                await _authService.UpdatePasswordAsync(userId, passwordDto.NewPassword);
                return Ok(new { message = "Cập nhật mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
        /// <summary>
        /// GET /api/users/{userId}/profile
        /// Returns the profile of another user (read-only on client side).
        /// </summary>
        [HttpGet("{userId}/profile")]
        public async Task<IActionResult> GetProfileByUserId([FromRoute] int userId)
        {
            try
            {
                if (userId <= 0) return BadRequest(new { message = "Invalid userId." });

                var profile = await _userService.GetProfileAsync(userId);
                if (profile == null) return NotFound(new { message = "User not found" });

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "An internal server error occurred.",
                        details = ex.Message
                    }
                );
            }
        }
    }
}
