using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using BusinessObjects.Interfaces;

namespace CapstoneProject_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DiscoveryController : ControllerBase
    {
        private readonly IDiscoveryService _discoveryService;
        private readonly ICampusContextService _campusContext;

        public DiscoveryController(IDiscoveryService discoveryService, ICampusContextService campusContext)
        {
            _discoveryService = discoveryService;
            _campusContext = campusContext;
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetLookingStudents(
            [FromQuery] int semesterId,
            [FromQuery] string? skill,
            [FromQuery] string? searchQuery,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var campusId = _campusContext.GetCurrentCampusId();
            if (campusId == null || campusId == 0) return BadRequest("Campus context not found.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _discoveryService.GetLookingStudentsAsync(
                semesterId, campusId.Value, userId, skill, searchQuery, page, pageSize);
            return Ok(result);
        }

        [HttpGet("teams")]
        public async Task<IActionResult> GetOpenTeams(
            [FromQuery] int semesterId,
            [FromQuery] string? skill,
            [FromQuery] string? searchQuery,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var campusId = _campusContext.GetCurrentCampusId();
            if (campusId == null || campusId == 0) return BadRequest("Campus context not found.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _discoveryService.GetOpenTeamsAsync(
                semesterId, campusId.Value, userId, skill, searchQuery, page, pageSize);
            return Ok(result);
        }

        [HttpGet("popular-skills")]
        [AllowAnonymous] // Allow anyone to see trending skills
        public async Task<IActionResult> GetPopularSkills()
        {
            var skills = await _discoveryService.GetPopularSkillsAsync();
            return Ok(skills);
        }


        [HttpGet("my-skills")]
        public async Task<IActionResult> GetMySkills()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var skills = await _discoveryService.GetUserSkillsAsync(userId);
            return Ok(skills);
        }

        [HttpGet("user/{userId}/skills")]
        // Intentionally public within auth context - skills are displayed on Discovery Board for all users
        public async Task<IActionResult> GetUserSkills(int userId)
        {
            var skills = await _discoveryService.GetUserSkillsAsync(userId);
            return Ok(skills);
        }

        [HttpPut("my-skills")]
        public async Task<IActionResult> UpdateMySkills([FromBody] UpdateUserSkillsRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _discoveryService.UpdateUserSkillsAsync(userId, request.Skills);
            return Ok();
        }

        [HttpPost("request-join/{teamId}")]
        [Authorize(Policy = "Student")]
        public async Task<IActionResult> RequestToJoin(int teamId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await _discoveryService.RequestToJoinAsync(userId, teamId);
                return Ok(new { message = "Join request sent successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred while processing the join request.");
            }
        }

        [HttpPost("cancel-request/{teamId}")]
        [Authorize(Policy = "Student")]
        public async Task<IActionResult> CancelJoinRequest(int teamId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await _discoveryService.CancelJoinRequestAsync(userId, teamId);
                return Ok(new { message = "Join request cancelled successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred while processing the cancel request.");
            }
        }
    }
}
