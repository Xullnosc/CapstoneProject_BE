using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LecturerController : ControllerBase
    {
        private readonly ILecturerService _lecturerService;

        public LecturerController(ILecturerService lecturerService)
        {
            _lecturerService = lecturerService;
        }

        [HttpGet]
        public async Task<ActionResult<BusinessObjects.DTOs.PagedResult<Lecturer>>> GetAll(int page = 1, int pageSize = 10, string? search = null)
        {
            var result = await _lecturerService.GetLecturersPaginatedAsync(page, pageSize, search);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Lecturer>> GetById(int id)
        {
            var result = await _lecturerService.GetLecturerByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Lecturer>> Create([FromBody] Lecturer lecturer)
        {
            await _lecturerService.AddLecturerAsync(lecturer);
            return CreatedAtAction(nameof(GetById), new { id = lecturer.LecturerId }, lecturer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Lecturer lecturer)
        {
            if (id != lecturer.LecturerId) return BadRequest();
            await _lecturerService.UpdateLecturerAsync(lecturer);
            return NoContent();
        }

        [HttpPut("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
        {
            await _lecturerService.ToggleLecturerStatusAsync(id, isActive);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _lecturerService.DeleteLecturerAsync(id);
            return NoContent();
        }
    }
}
