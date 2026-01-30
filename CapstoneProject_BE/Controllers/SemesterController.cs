using BusinessObjects.DTOs;
using BusinessObjects;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SemesterController : ControllerBase
    {
        private readonly ISemesterService _semesterService;

        public SemesterController(ISemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SemesterDTO>>> GetSemesters()
        {
            return await _semesterService.GetAllSemestersAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SemesterDTO>> GetSemester(int id)
        {
            var semester = await _semesterService.GetSemesterByIdAsync(id);
            if (semester == null)
            {
                return NotFound();
            }
            return semester;
        }

        [HttpPost]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<ActionResult<SemesterDTO>> CreateSemester(SemesterCreateDTO semesterCreateDTO)
        {
            try
            {
                var created = await _semesterService.CreateSemesterAsync(semesterCreateDTO);
                return CreatedAtAction(nameof(GetSemester), new { id = created.SemesterId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> UpdateSemester(int id, SemesterCreateDTO semesterCreateDTO)
        {
            if (id != semesterCreateDTO.SemesterId)
            {
                return BadRequest();
            }

            try
            {
                await _semesterService.UpdateSemesterAsync(semesterCreateDTO);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // [HttpDelete("{id}")] - Removed as per audit
        // Public method removed to prevent access

        [HttpPost("{id}/end")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> EndSemester(int id)
        {
            try
            {
                await _semesterService.EndSemesterAsync(id);
                return Ok(new { message = $"Semester {id} ended successfully. Data archived." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // In production, log this error
                return StatusCode(500, new { message = "An error occurred while ending the semester.", detail = ex.Message });
            }
        }
    }
}
