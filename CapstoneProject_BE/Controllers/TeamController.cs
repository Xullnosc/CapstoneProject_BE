using BusinessObjects;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Linq;
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
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }
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
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }
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
        public async Task<IActionResult> GetTeamsBySemester(int semesterId, [FromQuery] int? page = null, [FromQuery] int? limit = null)
        {
            if (page.HasValue && limit.HasValue)
            {
                var pagedResult = await _teamService.GetTeamsBySemesterPagedAsync(semesterId, page.Value, limit.Value);
                return Ok(pagedResult);
            }

            var teams = await _teamService.GetTeamsBySemesterAsync(semesterId);
            return Ok(teams);
        }
        [HttpGet("my-team")]
        public async Task<IActionResult> GetMyTeam()
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }
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
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }
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
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }
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

        [HttpDelete("{id}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int id, int memberId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }

                // Get team to verify leader
                var team = await _teamService.GetTeamByIdAsync(id, userId);
                if (team == null)
                {
                    return NotFound(new { message = "Team not found." });
                }

                // Check if current user is the leader
                if (team.LeaderId != userId)
                {
                    return StatusCode(403, new { message = "Only the team leader can remove members." });
                }

                // Check if trying to remove the leader
                if (team.LeaderId == memberId)
                {
                    return BadRequest(new { message = "Cannot remove the team leader. Transfer leadership first." });
                }

                // Check if member exists in team
                var memberExists = team.Members?.Any(m => m.StudentId == memberId) ?? false;
                if (!memberExists)
                {
                    return NotFound(new { message = "Member not found in this team." });
                }

                // Remove the member
                bool result = await _teamService.RemoveMemberAsync(id, memberId);

                if (!result)
                {
                    return NotFound(new { message = "Member could not be removed." });
                }

                return Ok(new { message = "Member removed successfully" });
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



        [HttpPut("{id}/leader")]
        public async Task<IActionResult> ChangeLeader(int id, [FromBody] ChangeLeaderDTO changeLeaderDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int currentLeaderId))
                {
                    return Unauthorized(new { message = "Invalid user identifier." });
                }

                bool result = await _teamService.ChangeLeaderAsync(id, currentLeaderId, changeLeaderDto.NewLeaderId);
                
                if (!result) return NotFound(new { message = "Team not found." });

                return Ok(new { message = "Leadership transferred successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

