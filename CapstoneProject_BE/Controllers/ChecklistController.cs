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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChecklistController : ControllerBase
    {
        private readonly IChecklistService _checklistService;

        public ChecklistController(IChecklistService checklistService)
        {
            _checklistService = checklistService;
        }

        /// <summary>GET /api/checklist - Get all checklist items (any authenticated user, e.g. for thesis evaluation view).</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChecklistDTO>>> GetAll()
        {
            var list = await _checklistService.GetAllAsync();
            return Ok(list);
        }

        /// <summary>GET /api/checklist/{id}</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ChecklistDTO>> GetById(int id)
        {
            var item = await _checklistService.GetByIdAsync(id);
            if (item == null)
                return NotFound(new { message = "Checklist item not found." });
            return Ok(item);
        }

        /// <summary>POST /api/checklist - Create (HOD only).</summary>
        [HttpPost]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<ActionResult<ChecklistDTO>> Create(ChecklistCreateDTO dto)
        {
            try
            {
                var created = await _checklistService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.ChecklistId }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>PUT /api/checklist/{id} - Update (HOD only).</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> Update(int id, ChecklistUpdateDTO dto)
        {
            try
            {
                await _checklistService.UpdateAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Checklist item not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>DELETE /api/checklist/{id} - Delete (HOD only).</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _checklistService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Checklist item not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
