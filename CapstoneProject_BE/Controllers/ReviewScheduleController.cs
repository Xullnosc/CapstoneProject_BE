using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/review-schedules")]
    [ApiController]
    public class ReviewScheduleController : ControllerBase
    {
        private readonly IReviewScheduleService _service;

        public ReviewScheduleController(IReviewScheduleService service)
        {
            _service = service;
        }

        [HttpGet("councils/{councilId}")]
        [Authorize(Roles = "Admin, Staff, HOD, Lecturer, Student")]
        public async Task<IActionResult> GetSchedulesByCouncil(int councilId)
        {
            try
            {
                var list = await _service.GetSchedulesByCouncilAsync(councilId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HOD, Lecturer")]
        public async Task<IActionResult> AddOrUpdateSchedule([FromBody] dynamic payload)
        {
            try
            {
                int councilId = payload.councilId;
                byte reviewRound = (byte)payload.reviewRound;
                DateTime scheduledDate = payload.scheduledDate;
                TimeSpan startTime = TimeSpan.Parse((string)payload.startTime);
                TimeSpan endTime = TimeSpan.Parse((string)payload.endTime);
                string meetLink = payload.meetLink;
                int setByLecturerId = payload.setByLecturerId;

                var created = await _service.AddOrUpdateScheduleAsync(
                    councilId, reviewRound, scheduledDate, startTime, endTime, meetLink, setByLecturerId);
                    
                return Ok(new { message = "Schedule updated and notifications sent", data = created });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
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
