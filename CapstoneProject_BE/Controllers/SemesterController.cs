using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.Helpers;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SemesterController : ControllerBase
    {
        private readonly ISemesterService _semesterService;
        private readonly IImportService _importService;
        private readonly IThesisEvaluationExportService _thesisEvaluationExportService;
        private readonly ICloudinaryHelper _cloudinaryHelper;

        public SemesterController(ISemesterService semesterService, IImportService importService, IThesisEvaluationExportService thesisEvaluationExportService, ICloudinaryHelper cloudinaryHelper)
        {
            _semesterService = semesterService;
            _importService = importService;
            _thesisEvaluationExportService = thesisEvaluationExportService;
            _cloudinaryHelper = cloudinaryHelper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SemesterDTO>>> GetSemesters()
        {
            return await _semesterService.GetAllSemestersAsync();
        }

        [HttpGet("paginated")]
        public async Task<ActionResult<PagedResult<SemesterDTO>>> GetSemestersPaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6)
        {
            try
            {
                var result = await _semesterService.GetAllSemestersPaginatedAsync(page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching semesters.", detail = ex.Message });
            }
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
    
        [HttpGet("current")]
        public async Task<ActionResult<SemesterDTO>> GetCurrentSemester()
        {
            var semester = await _semesterService.GetCurrentSemesterAsync();
            if (semester == null)
            {
                return NotFound(new { message = "No active semester found." });
            }
            return semester;
        }

        [HttpPost]
        [Authorize(Roles = CampusConstants.Roles.HOD + "," + CampusConstants.Roles.Admin)]
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = CampusConstants.Roles.HOD + "," + CampusConstants.Roles.Admin)]
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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

        [HttpPost("{id}/lock-submission")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> LockSubmission(int id)
        {
            try
            {
                await _semesterService.LockSubmissionAsync(id);
                return Ok(new { message = $"Semester {id} submission locked. Now in Review Thesis stage." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while locking submission.", detail = ex.Message });
            }
        }

        [HttpPost("{id}/lock-updates")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> LockUpdates(int id)
        {
            try
            {
                await _semesterService.LockAllUpdatesAsync(id);
                return Ok(new { message = $"Semester {id} updates locked. Now in Review Middle Semester stage." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while locking updates.", detail = ex.Message });
            }
        }

        public class AnnounceMidtermReviewRequest
        {
            public DateTime LockDate { get; set; }
        }

        [HttpPost("{id}/announce-midterm-review")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> AnnounceMidtermReview(int id, [FromBody] AnnounceMidtermReviewRequest request)
        {
            try
            {
                await _semesterService.AnnounceMidtermReviewAsync(id, request.LockDate);
                return Ok(new { message = $"Semester {id} midterm review lock date announced for {request.LockDate:yyyy-MM-dd}." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while announcing midterm review.", detail = ex.Message });
            }
        }

        [HttpPost("{id}/close")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> CloseSemester(int id)
        {
            try
            {
                await _semesterService.CloseSemesterAsync(id);
                return Ok(new { message = $"Semester {id} closed successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while closing the semester.", detail = ex.Message });
            }
        }

        // Keep /end as an alias for CloseSemester for backward compatibility
        [HttpPost("{id}/end")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> EndSemester(int id) => await CloseSemester(id);

        [HttpPost("{id}/whitelist/import")]
        [Authorize(Roles = CampusConstants.Roles.HOD)]
        public async Task<IActionResult> ImportWhitelist(
            int id,
            [FromForm] IFormFile file,
            [FromForm] string? commit = "false",
            [FromForm] List<int>? excludedRowNumbers = null,
            [FromForm] string? rowOverridesJson = null)
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
            var allowedExtensions = new[] { ".xlsx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(
                    new { message = "Invalid file type. Only .xlsx files are allowed." }
                );
            }

            try
            {
                using var stream = file.OpenReadStream();
                var uploaderEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
                if (string.IsNullOrWhiteSpace(uploaderEmail))
                {
                    return Unauthorized(new { message = "Unable to resolve uploader identity." });
                }

                // Deserialise optional row-level overrides supplied by the HOD to fix conflicts.
                List<WhitelistRowOverrideDTO>? rowOverrides = null;
                if (!string.IsNullOrWhiteSpace(rowOverridesJson))
                {
                    try
                    {
                        rowOverrides = System.Text.Json.JsonSerializer.Deserialize<List<WhitelistRowOverrideDTO>>(
                            rowOverridesJson,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch
                    {
                        return BadRequest(new { message = "rowOverridesJson is not valid JSON." });
                    }
                }

                var importResult = await _importService.ImportWhitelistFromExcel(stream, id, uploaderEmail, rowOverrides);

                if (excludedRowNumbers != null && excludedRowNumbers.Count > 0)
                {
                    var excludedSet = excludedRowNumbers.ToHashSet();
                    importResult.Items = importResult.Items
                        .Where(item => !excludedSet.Contains(item.RowNumber))
                        .ToList();
                }

                var commitFlag = false;
                if (!string.IsNullOrWhiteSpace(commit))
                {
                    bool.TryParse(commit, out commitFlag);
                }

                if (commitFlag)
                {
                    string fileUrl = "";
                    try
                    {
                        fileUrl = await _cloudinaryHelper.UploadFileAsync(file);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { message = "Failed to upload file to storage.", detail = ex.Message });
                    }

                    await _importService.SaveWhitelistBatchAsync(importResult, id, fileUrl, file.FileName, uploaderEmail);
                    return Ok(importResult);
                }

                return Ok(importResult);
            }
            catch (ArgumentException ex)
            {
                // Validation errors - safe to return to client
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException)
            {
                // Database/system state issues
                return BadRequest(new { message = "An error occurred while importing. Please verify the Excel file and try again." });
            }
            catch (Exception ex)
            {
                var message = ex.Message.Contains("file is not in Open XML format", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("not a valid package", StringComparison.OrdinalIgnoreCase)
                    ? "Unsupported Excel format. Please save the file as .xlsx and try again."
                    : "An error occurred while importing the whitelist. Please check the file format and try again.";

                // Generic exception - log internally, return generic message
                return StatusCode(
                    500,
                    new
                    {
                        message,
                    }
                );
            }
        }

        [HttpGet("{id}/whitelists")]
        public async Task<ActionResult<PagedResult<WhitelistDTO>>> GetWhitelists(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? role = null,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var result = await _semesterService.GetWhitelistsPaginatedAsync(id, page, pageSize, role, search);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        [HttpGet("{id}/export/evaluation")]
        [Authorize(Roles = CampusConstants.Roles.HOD + "," + CampusConstants.Roles.Admin)]
        public async Task<IActionResult> ExportEvaluation(int id, CancellationToken cancellationToken)
        {
            try
            {
                var semester = await _semesterService.GetSemesterByIdAsync(id);
                if (semester == null) return NotFound(new { message = "Semester not found." });

                if (!CampusConstants.SemesterStatus.IsLockedStage(semester.Status) && 
                    !CampusConstants.SemesterStatus.IsClosedStage(semester.Status))
                {
                    return BadRequest(new { message = "Cannot export evaluations while the semester is still Open. Please lock submission or close the semester before exporting." });
                }

                var request = new ReviewerSummarySheetRequestDTO { SemesterId = id };
                var bytes = await _thesisEvaluationExportService.GenerateWorkbookAsync(request, cancellationToken);
                var fileName = $"thesis-evaluation-semester-{id}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating the export.", detail = ex.Message });
            }
        }

        [HttpGet("{id}/orphaned-students")]
        [Authorize(Roles = CampusConstants.Roles.HOD + "," + CampusConstants.Roles.Admin)]
        public async Task<IActionResult> GetOrphanedStudents(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var orphaned = await _semesterService.GetOrphanedStudentsAsync(id, page, pageSize, search);
                return Ok(orphaned);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while fetching orphaned students." });
            }
        }

        [HttpGet("{id}/whitelist-batches")]
        [Authorize(Roles = CampusConstants.Roles.HOD + "," + CampusConstants.Roles.Admin)]
        public async Task<IActionResult> GetWhitelistBatches(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid semester ID." });
            }
            try
            {
                var batches = await _importService.GetImportBatchesBySemesterAsync(id);
                return Ok(batches);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching whitelist batches.", detail = ex.Message });
            }
        }
    }
}
