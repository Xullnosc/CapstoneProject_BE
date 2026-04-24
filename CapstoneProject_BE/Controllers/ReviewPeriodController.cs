using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/review-periods")]
    [ApiController]
    public class ReviewPeriodController : ControllerBase
    {
        private readonly IReviewPeriodService _reviewPeriodService;

        public ReviewPeriodController(IReviewPeriodService reviewPeriodService)
        {
            _reviewPeriodService = reviewPeriodService;
        }

        [HttpGet("semesters/{semesterId}")]
        [Authorize(Roles = "Admin, Staff, HOD, Lecturer")]
        public async Task<IActionResult> GetPeriodsBySemester(int semesterId)
        {
            try
            {
                var periods = await _reviewPeriodService.GetPeriodsBySemesterAsync(semesterId);
                return Ok(periods);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HOD")]
        public async Task<IActionResult> AddOrUpdatePeriod([FromBody] ReviewPeriodDTO dto)
        {
            try
            {
                var result = await _reviewPeriodService.AddOrUpdatePeriodAsync(dto.SemesterId, dto.ReviewRound, dto.StartDate, dto.EndDate);
                return Ok(new { message = "Period updated successfully", data = result });
            }
            catch (ArgumentException ex)
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
