using BusinessObjects;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = CampusConstants.Roles.Student)]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDTO createTeamDto)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var createdTeam = await _teamService.CreateTeamAsync(userId, createTeamDto);
                return CreatedAtAction(nameof(GetTeamById), new { id = createdTeam.TeamId }, createdTeam);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeamById(int id)
        {
            try 
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var team = await _teamService.GetTeamByIdAsync(id, userId);
                if (team == null) return NotFound(new { message = "Team not found" });
                return Ok(team);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("semester/{semesterId}")]
        public async Task<IActionResult> GetTeamsBySemester(int semesterId)
        {
            var teams = await _teamService.GetTeamsBySemesterAsync(semesterId);
            return Ok(teams);
        }
        [HttpGet("my-team")]
        public async Task<IActionResult> GetMyTeam()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var team = await _teamService.GetTeamByStudentIdAsync(userId);
                if (team == null) return NotFound(new { message = "You are not in any team" });
                return Ok(team);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/disband")]
        public async Task<IActionResult> DisbandTeam(int id)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                bool result = await _teamService.DisbandTeamAsync(id, userId);
                
                if (!result) return NotFound(new { message = "Team not found or could not be disbanded" });

                return Ok(new { message = "Team disbanded successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(int id, [FromForm] UpdateTeamDTO updateTeamDto)
        {
             try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var updatedTeam = await _teamService.UpdateTeamAsync(id, userId, updateTeamDto);
                return Ok(updatedTeam);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> LeaveTeam(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }

                // Check if user is leader
                var team = await _teamService.GetTeamByIdAsync(id, userId);
                if (team != null && team.LeaderId == userId)
                {
                    return BadRequest(new { message = "You are the team leader. You must transfer leadership before leaving the team." });
                }

                bool result = await _teamService.RemoveMemberAsync(id, userId);

                if (!result) return NotFound(new { message = "You are not in this team or could not leave." });

                return Ok(new { message = "Left team successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500,new { message = ex.Message });
            }
        }
    }
}

