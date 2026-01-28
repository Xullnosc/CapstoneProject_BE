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
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var invitations = await _invitationService.GetMyInvitationsAsync(userId);
                return Ok(invitations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptInvitation(int id)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/decline")]
        public async Task<IActionResult> DeclineInvitation(int id)
        {
             try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
