using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/mentor-invitation")]
    [ApiController]
    public class MentorInvitationController : ControllerBase
    {
        private readonly IMentorInvitationService _mentorInvitationService;
        private readonly IUserService _userService;

        public MentorInvitationController(IMentorInvitationService mentorInvitationService, IUserService userService)
        {
            _mentorInvitationService = mentorInvitationService;
            _userService = userService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        [HttpPost("send")]
        [Authorize(Roles = "Student")] // Leader is a Student role
        public async Task<IActionResult> SendMentorInvitation([FromBody] SendMentorInvitationRequest request)
        {
            try
            {
                int leaderId = GetCurrentUserId();
                var result = await _mentorInvitationService.SendMentorInvitationAsync(request.TeamId, leaderId, request.MentorEmail);
                return StatusCode(201, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found")) return NotFound(new { Message = ex.Message });
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("search-mentors")]
        [Authorize(Roles = "Student,Lecturer,HOD,Admin")]
        public async Task<IActionResult> SearchMentors([FromQuery] string? term, [FromQuery] int? teamId = null)
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                var lecturers = await _userService.SearchLecturersAsync(term ?? string.Empty, currentUserId, teamId);
                return Ok(lecturers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-invitations")]
        [Authorize(Roles = "Lecturer,HOD")]
        public async Task<IActionResult> GetMyInvitations([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                int mentorId = GetCurrentUserId();
                var result = await _mentorInvitationService.GetMentorInvitationsAsync(mentorId, pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("team/{teamId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetTeamMentorInvitations(int teamId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                int leaderId = GetCurrentUserId();
                var result = await _mentorInvitationService.GetTeamMentorInvitationsAsync(teamId, leaderId, pageIndex, pageSize);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found")) return NotFound(new { Message = ex.Message });
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/accept")]
        [Authorize(Roles = "Lecturer,HOD")]
        public async Task<IActionResult> AcceptInvitation(int id)
        {
            try
            {
                int mentorId = GetCurrentUserId();
                await _mentorInvitationService.AcceptMentorInvitationAsync(id, mentorId);
                return Ok(new { Message = "Invitation accepted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found")) return NotFound(new { Message = ex.Message });
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/decline")]
        [Authorize(Roles = "Lecturer,HOD")]
        public async Task<IActionResult> DeclineInvitation(int id)
        {
            try
            {
                int mentorId = GetCurrentUserId();
                await _mentorInvitationService.DeclineMentorInvitationAsync(id, mentorId);
                return Ok(new { Message = "Invitation declined successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found")) return NotFound(new { Message = ex.Message });
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("active-team-count")]
        [Authorize(Roles = "Lecturer,HOD")]
        public async Task<IActionResult> GetActiveTeamCount()
        {
            try
            {
                int mentorId = GetCurrentUserId();
                int count = await _mentorInvitationService.GetMentorActiveTeamCountAsync(mentorId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CancelInvitation(int id)
        {
            try
            {
                int leaderId = GetCurrentUserId();
                await _mentorInvitationService.CancelMentorInvitationAsync(id, leaderId);
                return Ok(new { Message = "Invitation cancelled successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found")) return NotFound(new { Message = ex.Message });
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
