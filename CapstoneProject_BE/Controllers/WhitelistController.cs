using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WhitelistController : ControllerBase
    {
        private readonly IWhitelistService _whitelistService;

        public WhitelistController(IWhitelistService whitelistService)
        {
            _whitelistService = whitelistService;
        }

        [HttpGet("role/{roleId}")]
        public async Task<ActionResult<IEnumerable<Whitelist>>> GetWhitelistByRole(int roleId)
        {
            var result = await _whitelistService.GetWhitelistByRoleAsync(roleId);
            return Ok(result);
        }

        [HttpPut("update-reviewer-status/{id}")]
        public async Task<IActionResult> UpdateReviewerStatus(int id, [FromBody] bool isReviewer)
        {
            try
            {
                await _whitelistService.UpdateReviewerStatusAsync(id, isReviewer);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Whitelist>> Add([FromBody] Whitelist whitelist)
        {
            var result = await _whitelistService.AddStudentToWhitelistAsync(whitelist);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Whitelist whitelist)
        {
            if (id != whitelist.WhitelistId) return BadRequest();
            await _whitelistService.UpdateWhitelistAsync(whitelist);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _whitelistService.DeleteWhitelistAsync(id);
            return NoContent();
        }
    }
}
