using BusinessObjects;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [ApiController]
    [Route("api/campuses")]
    [Authorize(Roles = CampusConstants.Roles.Admin)]
    public class CampusController : ControllerBase
    {
        private readonly ICampusService _campusService;

        public CampusController(ICampusService campusService)
        {
            _campusService = campusService;
        }

        [HttpGet]
        public async Task<ActionResult<List<CampusDTO>>> GetAllCampuses()
        {
            var result = await _campusService.GetAllCampusesAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CampusDTO>> GetCampusById(int id)
        {
            var result = await _campusService.GetCampusByIdAsync(id);
            if (result == null) return NotFound($"Campus ID {id} not found.");
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<CampusDTO>> CreateCampus([FromBody] CreateCampusDTO dto)
        {
            try
            {
                var result = await _campusService.CreateCampusAsync(dto);
                return CreatedAtAction(nameof(GetCampusById), new { id = result.CampusId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CampusDTO>> UpdateCampus(int id, [FromBody] UpdateCampusDTO dto)
        {
            try
            {
                var result = await _campusService.UpdateCampusAsync(id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCampus(int id)
        {
            try
            {
                await _campusService.DeleteCampusAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
