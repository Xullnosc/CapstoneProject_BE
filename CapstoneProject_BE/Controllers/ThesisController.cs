using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires authentication
    public class ThesisController : ControllerBase
    {
        private readonly IThesisService _thesisService;

        public ThesisController(IThesisService thesisService)
        {
            _thesisService = thesisService;
        }

        [HttpPost("propose")]
        public async Task<IActionResult> ProposeThesis([FromForm] ProposeThesisDTO req)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                if (emailClaim == null)
                {
                    return Unauthorized(new { Message = "Email claim not found in token." });
                }

                var email = emailClaim.Value;
                var thesis = await _thesisService.ProposeThesisAsync(req, email);

                return Ok(new { Message = "Thesis proposed successfully", Data = thesis });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { Message = errorMessage });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTheses()
        {
            try
            {
                var theses = await _thesisService.GetAllThesesAsync();
                return Ok(theses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
