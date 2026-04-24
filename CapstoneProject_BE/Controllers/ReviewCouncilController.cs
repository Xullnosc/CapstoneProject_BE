using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/review-councils")]
    [ApiController]
    public class ReviewCouncilController : ControllerBase
    {
        private readonly IReviewCouncilService _service;

        public ReviewCouncilController(IReviewCouncilService service)
        {
            _service = service;
        }

        [HttpGet("semesters/{semesterId}")]
        [Authorize(Roles = "Admin, Staff, HOD, Lecturer")]
        public async Task<IActionResult> GetCouncilsBySemester(int semesterId)
        {
            try
            {
                var list = await _service.GetCouncilsBySemesterAsync(semesterId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Staff, HOD, Lecturer")]
        public async Task<IActionResult> GetCouncilById(int id)
        {
            try
            {
                var council = await _service.GetCouncilByIdAsync(id);
                if (council == null) return NotFound(new { message = "Council not found" });
                return Ok(council);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> CreateCouncil([FromBody] CreateReviewCouncilDTO dto)
        {
            try
            {
                var created = await _service.CreateCouncilAsync(dto.SemesterId, dto.CouncilName, dto.CreatedBy);
                return Ok(new { message = "Council created", data = created });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> UpdateCouncil(int id, [FromBody] UpdateReviewCouncilDTO dto)
        {
            try
            {
                await _service.UpdateCouncilAsync(id, dto.CouncilName, dto.Status);
                return Ok(new { message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> DeleteCouncil(int id)
        {
            try
            {
                await _service.DeleteCouncilAsync(id);
                return Ok(new { message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("{id}/members")]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddCouncilMemberDTO dto)
        {
            try
            {
                await _service.AddMemberToCouncilAsync(id, dto.LecturerId, dto.Role);
                return Ok(new { message = "Member added successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/members/{lecturerId}")]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> RemoveMember(int id, int lecturerId)
        {
            try
            {
                await _service.RemoveMemberFromCouncilAsync(id, lecturerId);
                return Ok(new { message = "Member removed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("{id}/teams")]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> AddTeam(int id, [FromBody] AddCouncilTeamDTO dto)
        {
            try
            {
                await _service.AddTeamToCouncilAsync(id, dto.TeamId);
                return Ok(new { message = "Team added successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/teams/{teamId}")]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> RemoveTeam(int id, int teamId)
        {
            try
            {
                await _service.RemoveTeamFromCouncilAsync(id, teamId);
                return Ok(new { message = "Team removed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("auto-generate")]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> AutoGenerate([FromBody] AutoGenerateCouncilsDTO dto)
        {
            try
            {
                var createdById = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var councils = await _service.AutoGenerateCouncilsAsync(
                    dto.SemesterId,
                    dto.ReviewersPerCouncil,
                    createdById);

                return Ok(new
                {
                    message = $"{councils.Count} council(s) generated successfully.",
                    data = councils
                });
            }
            catch (InvalidOperationException ex)
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
