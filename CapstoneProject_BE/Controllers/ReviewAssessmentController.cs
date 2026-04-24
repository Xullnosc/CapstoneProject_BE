using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/review-assessments")]
    [ApiController]
    public class ReviewAssessmentController : ControllerBase
    {
        private readonly IReviewAssessmentService _service;

        public ReviewAssessmentController(IReviewAssessmentService service)
        {
            _service = service;
        }

        [HttpGet("councils/{councilId}/rounds/{round}/questions")]
        public async Task<IActionResult> GetQuestions(int councilId, byte round)
        {
            var list = await _service.GetQuestionsAsync(councilId, round);
            return Ok(list);
        }

        [HttpGet("councils/{councilId}/rounds/{round}/teams/{teamId}/results")]
        public async Task<IActionResult> GetResults(int councilId, byte round, int teamId)
        {
            var list = await _service.GetResultsAsync(councilId, round, teamId);
            return Ok(list);
        }

        [HttpPost("submit")]
        [Authorize(Roles = "Lecturer, HOD")]
        public async Task<IActionResult> SubmitResults([FromBody] List<ReviewQuestionResultDTO> results)
        {
            try
            {
                await _service.SubmitResultsAsync(results);
                return Ok(new { message = "Results submitted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("councils/{councilId}/teams/{teamId}/evaluate")]
        [Authorize(Roles = "Lecturer, HOD")]
        public async Task<IActionResult> EvaluateTeam(int councilId, int teamId)
        {
            try
            {
                var tracker = await _service.EvaluateTeamAsync(councilId, teamId);
                return Ok(new { message = "Team evaluated", data = tracker });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("councils/{councilId}/teams/{teamId}/override")]
        [Authorize(Roles = "HOD")]
        public async Task<IActionResult> OverrideStatus(int councilId, int teamId, [FromBody] dynamic payload)
        {
            try
            {
                byte round = (byte)payload.round;
                string status = payload.status;
                string comment = payload.comment;

                await _service.OverrideTeamStatusAsync(councilId, teamId, round, status, comment);
                return Ok(new { message = "Status overridden successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
