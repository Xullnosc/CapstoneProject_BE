using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "HOD")]
    public class SemesterController : ControllerBase
    {
        private readonly ISemesterService _semesterService;

        public SemesterController(ISemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Semester>>> GetSemesters()
        {
            return await _semesterService.GetAllSemestersAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Semester>> GetSemester(int id)
        {
            var semester = await _semesterService.GetSemesterByIdAsync(id);
            if (semester == null)
            {
                return NotFound();
            }
            return semester;
        }

        [HttpPost]
        public async Task<ActionResult<Semester>> CreateSemester(Semester semester)
        {
            var created = await _semesterService.CreateSemesterAsync(semester);
            return CreatedAtAction(nameof(GetSemester), new { id = created.SemesterId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSemester(int id, Semester semester)
        {
            if (id != semester.SemesterId)
            {
                return BadRequest();
            }
            await _semesterService.UpdateSemesterAsync(semester);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSemester(int id)
        {
            await _semesterService.DeleteSemesterAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/end")]
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
