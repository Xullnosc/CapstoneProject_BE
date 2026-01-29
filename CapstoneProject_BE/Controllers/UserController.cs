using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = CampusConstants.Roles.Student)]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchStudents([FromQuery] string term, [FromQuery] int? teamId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return BadRequest(new { message = "Search term cannot be empty" });
                }

                var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
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
    }
}
