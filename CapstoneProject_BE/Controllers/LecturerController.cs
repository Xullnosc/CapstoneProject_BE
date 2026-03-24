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
        public async Task<ActionResult<BusinessObjects.DTOs.PagedResult<Lecturer>>> GetAll(int page = 1, int pageSize = 10, string? search = null)
        {
            var result = await _lecturerService.GetLecturersPaginatedAsync(page, pageSize, search);
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
                return BadRequest("pageIndex must be greater than 0.");
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                return BadRequest("pageSize must be between 1 and 100.");
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
            var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");
            if (roleClaim?.Value == CampusConstants.Roles.HOD)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var profile = await _userService.GetProfileAsync(userId);
                    if (profile != null)
                    {
                        lecturer.Campus = profile.Campus;
                    }
                }
            }

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
