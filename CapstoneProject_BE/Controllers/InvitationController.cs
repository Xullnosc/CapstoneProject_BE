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
    [Route("api/invitation")] // Should match frontend expectations: 'invitation' or 'invitations'? Frontend uses /invitation/{id}/..
    [ApiController]
    [Authorize(Roles = CampusConstants.Roles.Student)]
    public class InvitationController : ControllerBase
    {
        private readonly ITeamInvitationService _invitationService;

        public InvitationController(ITeamInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        [HttpGet("my-invitations")]
        public async Task<IActionResult> GetMyInvitations()
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }
                var invitations = await _invitationService.GetMyInvitationsAsync(userId);
                return Ok(invitations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred.", details = ex.Message });
            }
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptInvitation(int id)
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }
                await _invitationService.AcceptInvitationAsync(id, userId);
                return Ok(new { message = "Invitation accepted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred.", details = ex.Message });
            }
        }

        [HttpPost("{id}/decline")]
        public async Task<IActionResult> DeclineInvitation(int id)
        {
             try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }
                await _invitationService.DeclineInvitationAsync(id, userId);
                return Ok(new { message = "Invitation declined successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred.", details = ex.Message });
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendInvitation([FromBody] DTOs.Requests.SendInvitationRequest request)
        {
             try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }
                
                if (string.IsNullOrWhiteSpace(request.StudentCodeOrEmail))
                {
                    return BadRequest(new { message = "Student Code or Email is required" });
                }

                var invitation = await _invitationService.SendInvitationAsync(request.TeamId, userId, request.StudentCodeOrEmail);
                return Ok(new { message = "Invitation sent successfully", data = invitation });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred.", details = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelInvitation(int id)
        {
             try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }

                await _invitationService.CancelInvitationAsync(id, userId);
                return Ok(new { message = "Invitation cancelled successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred.", details = ex.Message });
            }
        }
    }
}
