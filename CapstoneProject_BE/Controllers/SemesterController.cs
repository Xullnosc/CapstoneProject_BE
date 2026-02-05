using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SemesterController : ControllerBase
    {
        private readonly ISemesterService _semesterService;
        private readonly IImportService _importService;

        public SemesterController(ISemesterService semesterService, IImportService importService)
        {
            _semesterService = semesterService;
            _importService = importService;
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
        public async Task<ActionResult<SemesterDTO>> CreateSemester(
            SemesterCreateDTO semesterCreateDTO
        )
        {
            try
            {
                var created = await _semesterService.CreateSemesterAsync(semesterCreateDTO);
                return CreatedAtAction(
                    nameof(GetSemester),
                    new { id = created.SemesterId },
                    created
                );
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

        [HttpPost("{id}/start")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> StartSemester(int id)
        {
            try
            {
                await _semesterService.StartSemesterAsync(id);
                return Ok(
                    new
                    {
                        message = $"Semester {id} started successfully. Previous active semester (if any) has been ended.",
                    }
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // In production, log this error
                return StatusCode(
                    500,
                    new
                    {
                        message = "An error occurred while starting the semester.",
                        detail = ex.Message,
                    }
                );
            }
        }

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
                return StatusCode(
                    500,
                    new
                    {
                        message = "An error occurred while ending the semester.",
                        detail = ex.Message,
                    }
                );
            }
        }

        [HttpPost("{id}/whitelist/import")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> ImportWhitelist(int id, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { message = "File size exceeds the 5 MB limit." });
            }
            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(
                    new { message = "Invalid file type. Only .xlsx and .xls files are allowed." }
                );
            }

            try
            {
                using var stream = file.OpenReadStream();
                var importedWhiteLists = await _importService.ImportWhitelistFromExcel(stream);
                return Ok(importedWhiteLists);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // In production, log this error
                return StatusCode(
                    500,
                    new
                    {
                        message = "An error occurred while importing the whitelist.",
                        detail = ex.Message,
                    }
                );
            }
        }
    }
}
