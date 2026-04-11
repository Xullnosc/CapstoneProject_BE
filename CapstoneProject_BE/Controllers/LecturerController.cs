using BusinessObjects.Models;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "HodOrAdmin")]
    public class LecturerController : ControllerBase
    {
        private readonly ILecturerService _lecturerService;
        private readonly IUserService _userService;

        public LecturerController(ILecturerService lecturerService, IUserService userService)
        {
            _lecturerService = lecturerService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<BusinessObjects.DTOs.PagedResult<Lecturer>>> GetAll(int page = 1, int pageSize = 10, string? search = null, int? campusId = null)
        {
            var result = await _lecturerService.GetLecturersPaginatedAsync(page, pageSize, search, campusId);
            return Ok(result);
        }

        [HttpGet("campus/{campus}")]
        public async Task<ActionResult<PagedResult<Lecturer>>> GetByCampus(string campus, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(campus))
            {
                return BadRequest("Campus is required.");
            }

            if (pageIndex <= 0)
            {
                pageIndex = 1;
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                pageSize = 100;
            }

            var result = await _lecturerService.GetLecturersByCampusAsync(campus, pageIndex, pageSize);
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
            try
            {
                await _lecturerService.AddLecturerAsync(lecturer);
                return CreatedAtAction(nameof(GetById), new { id = lecturer.LecturerId }, lecturer);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Lecturer lecturer)
        {
            if (id != lecturer.LecturerId) return BadRequest();
            try
            {
                await _lecturerService.UpdateLecturerAsync(lecturer);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
        {
            await _lecturerService.ToggleLecturerStatusAsync(id, isActive);
            return NoContent();
        }

        [HttpPut("toggle-reviewer/{id}")]
        public async Task<IActionResult> ToggleReviewer(int id, [FromBody] bool isReviewer)
        {
            await _lecturerService.ToggleReviewerAsync(id, isReviewer);
            return NoContent();
        }

        [HttpPut("toggle-hod/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleHod(int id, [FromBody] bool isHod)
        {
            try
            {
                await _lecturerService.ToggleHodAsync(id, isHod);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _lecturerService.DeleteLecturerAsync(id);
            return NoContent();
        }
    }
}
