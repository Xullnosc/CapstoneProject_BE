using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BusinessObjects;
using BusinessObjects.AI.Models;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Repositories;
using Services.AI.Configuration;
using Services.Helpers;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Services
{
    public class ThesisService : IThesisService
    {
        private readonly IThesisRepository _thesisRepository;
        private readonly IThesisReviewRepository _thesisReviewRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryHelper _cloudinaryHelper;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ITeamInvitationRepository _teamInvitationRepository;
        private readonly IMapper _mapper;
        private readonly ISystemParameterService _systemParameterService;
        private readonly IChecklistRepository? _checklistRepository;
        private readonly IAIService? _aiService;
        private readonly IUserAISettingsService? _userAiSettingsService;
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly ILogger<ThesisService>? _logger;

        private const int MaxExtractedChars = 24000;

        public ThesisService(
            IThesisRepository thesisRepository,
            IThesisReviewRepository thesisReviewRepository,
            ITeamRepository teamRepository,
            IUserRepository userRepository,
            ICloudinaryHelper cloudinaryHelper,
            ISemesterRepository semesterRepository,
            ILecturerRepository lecturerRepository,
            ITeamInvitationRepository teamInvitationRepository,
            IMapper mapper,
            ISystemParameterService systemParameterService,
            IChecklistRepository? checklistRepository = null,
            IAIService? aiService = null,
            IUserAISettingsService? userAiSettingsService = null,
            IHttpClientFactory? httpClientFactory = null,
            ILogger<ThesisService>? logger = null
        )
        {
            _thesisRepository = thesisRepository;
            _thesisReviewRepository = thesisReviewRepository;
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
            _semesterRepository = semesterRepository;
            _lecturerRepository = lecturerRepository;
            _teamInvitationRepository = teamInvitationRepository;
            _mapper = mapper;
            _systemParameterService = systemParameterService;
            _checklistRepository = checklistRepository;
            _aiService = aiService;
            _userAiSettingsService = userAiSettingsService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        #region Core Thesis Lifecycle

        public async Task<Thesis> ProposeThesisAsync(ProposeThesisDTO req, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new Exception("User not found");

            // Resolve target user (HOD can propose for another lecturer)
            User targetUser = user;
            bool isStaff = user.Role?.RoleName == CampusConstants.Roles.Lecturer || 
                           user.Role?.RoleName == CampusConstants.Roles.HOD || 
                           user.Role?.RoleName == CampusConstants.Roles.Admin;

            bool isHodActingForOther = (user.Role?.RoleName == CampusConstants.Roles.HOD || user.Role?.RoleName == CampusConstants.Roles.Admin) && 
                                       req.AuthorId.HasValue && req.AuthorId != user.UserId;
            
            if (isHodActingForOther)
            {
                var author = await _userRepository.GetByIdAsync(req.AuthorId!.Value);
                if (author == null) throw new Exception("Author user not found.");
                targetUser = author;
            }

            Team? team = null;

            // Prevent multiple theses per leader, except for Lecturers.
            // Allow re-proposing if all previous theses are Cancelled or Rejected.
            var existingTheses = await _thesisRepository.GetThesesByUserIdAsync(targetUser.UserId);
            var hasActiveThesis = existingTheses.Any(t =>
                t.Status != "Cancelled" && t.Status != "Rejected"
            );
            
            // Skip multiple-check for HOD-submitted topics or if target is Lecturer/HOD/Admin
            bool isTargetStaff = targetUser.Role?.RoleName == CampusConstants.Roles.Lecturer || 
                                 targetUser.Role?.RoleName == CampusConstants.Roles.HOD || 
                                 targetUser.Role?.RoleName == CampusConstants.Roles.Admin;
            
            if (hasActiveThesis && !isTargetStaff)
            {
                throw new InvalidOperationException(
                    "This user has already proposed a thesis. They cannot propose more than one."
                );
            }

            // Students must be the team leader and the team must have at least 4 members.
            if (!isTargetStaff)
            {
                // Check if thesis registration is open
                var isRegistrationOpen = await _systemParameterService.GetBoolAsync("THESIS_REGISTRATION_OPEN", true);
                if (!isRegistrationOpen)
                    throw new InvalidOperationException("Thesis registration is currently closed by administrator.");

                team = await _teamRepository.GetActiveTeamByStudentIdAsync(targetUser.UserId);
                if (team == null)
                    throw new InvalidOperationException(
                        "Target user must be in an active team to propose a thesis."
                    );

                if (team.LeaderId != targetUser.UserId)
                    throw new InvalidOperationException(
                        "Only the team leader can propose a thesis."
                    );

                if (!team.IsSpecial && team.Teammembers.Count < 4)
                     throw new InvalidOperationException(
                         $"Target team must have at least 4 members to propose a thesis unless marked as special. Current members: {team.Teammembers.Count}."
                     );

                // [NEW] Check if team already has a Registered thesis
                var teamTheses = await _thesisRepository.GetThesesByTeamIdAsync(team.TeamId);
                if (teamTheses.Any(t => string.Equals(t.Status, "Registered", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Team của bạn đã được đăng ký vào một đề tài khác. Không thể nộp thêm đề tài mới.");
                }
            }

            string? fileUrl = null;
            if (req.File != null)
            {
                var limitMb = await _systemParameterService.GetIntAsync("FILE_SIZE_LIMIT_MB", 10);
                if (req.File.Length > limitMb * 1024L * 1024L)
                    throw new InvalidOperationException($"File size exceeds the {limitMb}MB limit set by administrator.");
                fileUrl = await _cloudinaryHelper.UploadFileAsync(req.File);
            }

            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester == null)
            {
                throw new InvalidOperationException("Không tìm thấy kỳ học hiện tại để nộp đề tài.");
            }

            // [LIFECYCLE GUARD] Chỉ cho phép nộp mới trong giai đoạn Open
            if (!CampusConstants.SemesterStatus.IsOpenStage(currentSemester.Status))
            {
                throw new InvalidOperationException($"Kỳ học đang ở trạng thái '{currentSemester.Status}'. Chỉ có thể nộp đề tài mới khi kỳ học đang mở (Open).");
            }

            var hasAssignedMentor =
                team?.MentorId.HasValue == true || team?.MentorId2.HasValue == true;

            var thesis = new Thesis
            {
                CampusId = targetUser.CampusId ?? 1,
                ThesisId = Guid.NewGuid().ToString(),
                Title = string.IsNullOrWhiteSpace(req.Title)
                    ? (
                        req.File != null
                            ? System.IO.Path.GetFileNameWithoutExtension(req.File.FileName)
                            : "Untitled"
                    )
                    : req.Title.Trim(),
                ShortDescription = req.ShortDescription,
                UserId = targetUser.UserId,
                FileUrl = fileUrl,
                Status =
                    isTargetStaff || isHodActingForOther
                        ? "Reviewing"
                        : (hasAssignedMentor ? "Reviewing" : "On Mentor Inviting"),
                SemesterId = currentSemester?.SemesterId,
                UpDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow,

                ThesisNameEn = req.ThesisNameEn,
                ThesisNameVi = req.ThesisNameVi,
                Abbreviation = req.Abbreviation,
                IsFromEnterprise = req.IsFromEnterprise,
                EnterpriseName = req.IsFromEnterprise ? req.EnterpriseName : null,
                IsApplied = req.IsApplied,
                IsAppUsed = req.IsAppUsed,
                OriginalAuthorId = targetUser.UserId,
            };

            // Set TeamId based on role (Staff don't have teams)
            if (!isTargetStaff)
            {
                if (team != null)
                {
                    thesis.TeamId = team.TeamId;
                }
            }
            var createdThesis = await _thesisRepository.CreateThesisAsync(thesis);
            return createdThesis;
        }

        public async Task<IEnumerable<Thesis>> GetAllThesesAsync() =>
            await _thesisRepository.GetAllThesesAsync();

        public async Task<Thesis?> GetThesisByIdAsync(string id) =>
            await _thesisRepository.GetThesisByIdAsync(id);

        public async Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId) =>
            await _thesisRepository.GetThesesByUserIdAsync(userId);

        public async Task UpdateThesisStatusAsync(string thesisId, string status)
        {
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new Exception("Thesis not found");

            thesis.Status = status;
            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);
        }

        #endregion

        #region Owner Thesis Management

        /// <summary>
        /// Upload a new file version for a thesis.
        /// - Only the owner can update their thesis.
        /// - Creates a ThesisHistory record for version tracking.
        /// - Updates FileUrl and UpdateDate on the original Thesis.
        /// </summary>
        public async Task<ThesisDTO> UpdateThesisAsync(
            string thesisId,
            UpdateThesisDTO req,
            string email
        )
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var thesis = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            // [LIFECYCLE GUARD] Chỉ cho phép sửa/revision đề tài ở giai đoạn Open
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester != null && !CampusConstants.SemesterStatus.IsOpenStage(currentSemester.Status))
            {
                throw new InvalidOperationException($"Kỳ học đang ở trạng thái '{currentSemester.Status}'. Chỉ có thể cập nhật đề tài khi kỳ học đang mở (Open).");
            }

            // Only the owner can update
            if (thesis.UserId != user.UserId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to update this thesis."
                );

            // Only allow updates when status is 'Need Update'
            if (!string.Equals(thesis.Status, "Need Update", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Cannot update thesis when it is in '{thesis.Status}' state. Updates are only allowed during 'Need Update' state.");
            }

            // Upload new file to Cloudinary (if provided)
            if (req.File != null)
            {
                var limitMb = await _systemParameterService.GetIntAsync("FILE_SIZE_LIMIT_MB", 10);
                if (req.File.Length > limitMb * 1024L * 1024L)
                    throw new InvalidOperationException($"File size exceeds the {limitMb}MB limit set by administrator.");
                string newFileUrl = await _cloudinaryHelper.UploadFileAsync(req.File);

                // Calculate next version number
                int nextVersion = thesis.ThesisHistories.Any()
                    ? thesis.ThesisHistories.Max(h => h.VersionNumber) + 1
                    : 1;

                // Create history record
                var history = new ThesisHistory
                {
                    ThesisId = thesis.ThesisId,
                    FileUrl = newFileUrl,
                    VersionNumber = nextVersion,

                    UploadedBy = user.UserId,
                    CreatedAt = DateTime.UtcNow,
                };

                await _thesisRepository.AddThesisHistoryAsync(history);

                // Update thesis with new file URL and transition status
                thesis.FileUrl = newFileUrl;
                thesis.Status = "Reviewing"; // Re-enter review queue
            }
            else
            {
                // If it was metadata-only update while in Need Update, we still move it to Reviewing 
                // because the author signals they have addressed the "Need Update" feedback.
                thesis.Status = "Reviewing";
            }

            // Update optional metadata fields
            if (!string.IsNullOrWhiteSpace(req.Title))
            {
                thesis.Title = req.Title.Trim();
            }
            else if (req.File != null)
            {
                // Sync Title with filename if a new file is uploaded and no title provided
                thesis.Title = System.IO.Path.GetFileNameWithoutExtension(req.File.FileName);
            }

            if (req.ShortDescription != null)
                thesis.ShortDescription = req.ShortDescription.Trim();

            // Update new reporting fields
            if (req.ThesisNameEn != null) thesis.ThesisNameEn = req.ThesisNameEn.Trim();
            if (req.ThesisNameVi != null) thesis.ThesisNameVi = req.ThesisNameVi.Trim();
            if (req.Abbreviation != null) thesis.Abbreviation = req.Abbreviation.Trim();
            if (req.IsFromEnterprise.HasValue) thesis.IsFromEnterprise = req.IsFromEnterprise.Value;
            if (req.IsFromEnterprise == true) thesis.EnterpriseName = req.EnterpriseName?.Trim();
            else if (req.IsFromEnterprise == false) thesis.EnterpriseName = null;
            if (req.IsApplied.HasValue) thesis.IsApplied = req.IsApplied.Value;
            if (req.IsAppUsed.HasValue) thesis.IsAppUsed = req.IsAppUsed.Value;

            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);

            // Reload with fresh histories
            var updated = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            return _mapper.Map<ThesisDTO>(updated!);
        }

        /// <summary>
        /// Cancel a thesis proposal.
        /// Only the owner can cancel it.
        /// </summary>
        public async Task<ThesisDTO> CancelThesisAsync(string thesisId, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var thesis = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            // Only the owner can cancel
            if (thesis.UserId != user.UserId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to cancel this thesis."
                );

            // [LIFECYCLE GUARD] Chỉ cho phép hủy đề tài ở giai đoạn Open
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester != null && !CampusConstants.SemesterStatus.IsOpenStage(currentSemester.Status))
            {
                throw new InvalidOperationException($"Kỳ học đang ở trạng thái '{currentSemester.Status}'. Không thể hủy đề tài.");
            }

            // Only cancel if not already matched or published
            // UPDATED: Allow cancellation if 'Need Update'
            var cancellable = new[]
            {
                "Reviewing",
                "Registered",
                "On Mentor Inviting",
                "Need Update",
            };
            if (!cancellable.Contains(thesis.Status))
                throw new InvalidOperationException(
                    $"Cannot cancel a thesis that is '{thesis.Status}'."
                );

            thesis.Status = "Cancelled";
            thesis.UpdateDate = DateTime.UtcNow;

            await _thesisRepository.UpdateThesisAsync(thesis);

            return _mapper.Map<ThesisDTO>(thesis);
        }


        /// <summary>
        /// Get all theses owned by the logged-in student.
        /// </summary>
        public async Task<IEnumerable<ThesisDTO>> GetMyThesesAsync(
            string email,
            string? status = null,
            string? searchTitle = null
        )
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var ownerIds = new HashSet<int>();
            var teamIds = new HashSet<int>();

            bool isStaff = user.Role?.RoleName == CampusConstants.Roles.Lecturer || 
                           user.Role?.RoleName == CampusConstants.Roles.HOD;

            if (isStaff)
            {
                // Lecturer view: see their own proposals
                ownerIds.Add(user.UserId);

                // Lecturer/Mentor view: see theses of all teams they mentor in current semester
                var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
                if (currentSemester != null)
                {
                    var allTeams = await _teamRepository.GetBySemesterAsync(
                        currentSemester.SemesterId
                    );
                    var mentoredTeams = allTeams
                        .Where(t =>
                            (t.MentorId == user.UserId || t.MentorId2 == user.UserId)
                            && t.Status != CampusConstants.TeamStatus.Disbanded
                        )
                        .ToList();

                    foreach (var t in mentoredTeams)
                    {
                        teamIds.Add(t.TeamId);
                        ownerIds.Add(t.LeaderId); // Fallback: See leader's thesis if TeamId is null
                    }

                    // Add theses from teams that have a Pending mentor invitation for this lecturer
                    var pendingInvitations =
                        await _teamInvitationRepository.GetPendingMentorInvitationsByMentorIdAsync(
                            user.UserId
                        );
                    var pendingTeamIds = pendingInvitations
                        .Select(i => i.TeamId)
                        .Distinct()
                        .ToList();

                    var pendingTeams = allTeams
                        .Where(t =>
                            pendingTeamIds.Contains(t.TeamId)
                            && t.Status != CampusConstants.TeamStatus.Disbanded
                        )
                        .ToList();

                    foreach (var t in pendingTeams)
                    {
                        teamIds.Add(t.TeamId);
                        ownerIds.Add(t.LeaderId); // Also see leader's thesis for invitation review
                    }
                }
            }
            else
            {
                // Student view: always see their own proposed theses
                ownerIds.Add(user.UserId);

                // check if in a team
                var team = await _teamRepository.GetActiveTeamByStudentIdAsync(user.UserId);
                if (team != null)
                {
                    // See the leader's theses of their current team
                    ownerIds.Add(team.LeaderId);
                    // AND see any thesis explicitly assigned to this team (e.g. proposed by a Mentor)
                    teamIds.Add(team.TeamId);
                }
            }

            if (!ownerIds.Any() && !teamIds.Any())
                return new List<ThesisDTO>();

            var currentSem = await _semesterRepository.GetCurrentSemesterAsync();
            var theses = await _thesisRepository.GetThesesByOwnerOrTeamAsync(
                ownerIds,
                teamIds,
                currentSem?.SemesterId
            );

            // Apply filtering in memory
            if (!string.IsNullOrWhiteSpace(status))
            {
                theses = theses.Where(t =>
                    string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase)
                );
            }
            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                theses = theses.Where(t =>
                    (t.Title != null && t.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase)) ||
                    (t.User != null && t.User.FullName != null && t.User.FullName.Contains(searchTitle, StringComparison.OrdinalIgnoreCase))
                );
            }

            return _mapper.Map<IEnumerable<ThesisDTO>>(theses);
        }

        #endregion

        #region Review Workflow

        public async Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId)
        {
            return await _thesisReviewRepository.GetReviewStatusAsync(thesisId);
        }

        public async Task<PagedResult<ThesisReviewTimelineEventDTO>> GetReviewTimelineAsync(
            string thesisId,
            int pageIndex = 1,
            int pageSize = 10
        )
        {
            var timeline = await _thesisReviewRepository.GetTimelineAsync(
                thesisId,
                pageIndex,
                pageSize
            );

            // Populate avatars from Lecturers table if missing or to ensure latest
            var emails = timeline.Items
                .Select(e => e.ActorEmail)
                .Concat(timeline.Items.SelectMany(e => e.Comments).Select(c => c.AuthorEmail))
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!.Trim())
                .Distinct()
                .ToList();

            if (emails.Any())
            {
                var lecturers = await _lecturerRepository.GetByEmailsAsync(emails);
                var avatarMap = lecturers
                    .Where(l => !string.IsNullOrEmpty(l.Avatar))
                    .ToDictionary(l => l.Email.Trim().ToLower(), l => l.Avatar);

                foreach (var evt in timeline.Items)
                {
                    if (!string.IsNullOrEmpty(evt.ActorEmail))
                    {
                        var key = evt.ActorEmail.Trim().ToLower();
                        if (avatarMap.TryGetValue(key, out var avatar))
                        {
                            evt.ActorAvatar = avatar;
                        }
                    }

                    foreach (var comment in evt.Comments)
                    {
                        if (!string.IsNullOrEmpty(comment.AuthorEmail))
                        {
                            var key = comment.AuthorEmail.Trim().ToLower();
                            if (avatarMap.TryGetValue(key, out var avatar))
                            {
                                comment.AuthorAvatar = avatar;
                            }
                        }
                    }
                }
            }

            return timeline;
        }

        public async Task<ThesisReviewTimelineCommentDTO> AddReviewCommentAsync(
            string thesisId,
            int actorUserId,
            CreateThesisReviewCommentDTO dto
        )
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var user = await _userRepository.GetByIdAsync(actorUserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
            {
                throw new KeyNotFoundException("Thesis not found.");
            }

            var actorRole = await ResolveTimelineActorRoleAsync(user, thesis);
            return await _thesisReviewRepository.AddCommentAsync(
                thesisId,
                actorUserId,
                actorRole,
                dto
            );
        }

        public async Task<ThesisReviewStatusDTO> AssignReviewersAsync(
            string thesisId,
            int[] reviewerIds,
            int assignedByUserId
        )
        {
            if (reviewerIds == null || reviewerIds.Distinct().Count() != 2)
                throw new ArgumentException("Exactly 2 reviewers are required.");

            var distinct = reviewerIds.Distinct().ToArray();
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            if (distinct.Contains(thesis.UserId))
                throw new ArgumentException(
                    "Thesis proposer cannot be a reviewer for their own thesis."
                );

            // Reset status to Reviewing when (re)assigning reviewers
            thesis.Status = "Reviewing";
            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);

            // Initialize the horizontal review row
            await _thesisReviewRepository.InitializeReviewersAsync(
                thesisId,
                distinct[0],
                distinct[1],
                assignedByUserId
            );

            // NOTE: Auto-pass for proposer-reviewers happens in SubmitReviewerDecisionAsync,
            // not here, so assignment is a clean, neutral operation.

            var status = await _thesisReviewRepository.GetReviewStatusAsync(thesisId);
            await ApplyDecisionToThesisStatusAsync(thesisId, status);
            return status;
        }

        public async Task<ThesisReviewStatusDTO> SubmitReviewerDecisionAsync(
            string thesisId,
            int reviewerUserId,
            SubmitThesisDecisionDTO dto
        )
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            var decision = (dto.Decision ?? "").Trim();
            if (
                !string.Equals(decision, "Pass", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(decision, "Fail", StringComparison.OrdinalIgnoreCase)
            )
                throw new ArgumentException("Invalid decision");

            if (
                string.Equals(decision, "Fail", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(dto.Comment)
            )
                throw new ArgumentException("Fail reason is required.");

            // 1. Verify the user is a Lecturer with Reviewer permissions
            var user = await _userRepository.GetByIdAsync(reviewerUserId);
            var isHod = user?.Role?.RoleName == CampusConstants.Roles.HOD;
            var isLecturer = user?.Role?.RoleName == CampusConstants.Roles.Lecturer;

            if (user == null || (!isLecturer && !isHod))
                throw new UnauthorizedAccessException("Only reviewers or HOD can submit reviews.");

            var lecturer = await _lecturerRepository.GetByEmailAsync(user.Email);
            if (lecturer == null || (!lecturer.IsReviewer && !isHod))
                throw new UnauthorizedAccessException("You do not have reviewer permissions.");

            // 2. Ensure thesis is in Reviewing state
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            if (thesis.UserId == reviewerUserId)
                throw new UnauthorizedAccessException(
                    "You cannot review your own thesis proposal."
                );

            if (lecturer != null && (thesis.MentorId1 == lecturer.LecturerId || thesis.MentorId2 == lecturer.LecturerId))
            {
                throw new UnauthorizedAccessException("Mentors cannot submit reviews for their own teams.");
            }

            if (
                !isHod
                && !string.Equals(thesis.Status, "Reviewing", StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new InvalidOperationException(
                    $"Cannot submit review decision when thesis is in '{thesis.Status}' state. Decisions are only allowed during 'Reviewing' state."
                );
            }

            var currentReviewStatus = await _thesisReviewRepository.GetReviewStatusAsync(thesisId);
            var assignedReviewers =
                currentReviewStatus?.Reviewers ?? new List<ReviewerProgressDTO>();
            bool isAssigned = assignedReviewers.Any(r => r.UserId == reviewerUserId);

            // Auto-assignment happens in the DAO. Only block if they aren't assigned AND slots are full.
            if (!isHod && assignedReviewers.Count >= 2)
            {
                throw new UnauthorizedAccessException(
                    "You are not an assigned reviewer for this thesis."
                );
            }

            await _thesisReviewRepository.UpsertReviewerReviewAsync(
                thesisId,
                reviewerUserId,
                decision.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Pass" : "Fail",
                dto.Comment?.Trim(),
                dto.CheckedChecklistIds
            );

            // update thesis status based on decisions
            var status = await _thesisReviewRepository.GetReviewStatusAsync(thesisId);
            await ApplyDecisionToThesisStatusAsync(thesisId, status);
            return await _thesisReviewRepository.GetReviewStatusAsync(thesisId);
        }

        public async Task<ThesisReviewStatusDTO> SubmitHodDecisionAsync(
            string thesisId,
            int hodUserId,
            SubmitThesisDecisionDTO dto
        )
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            var decision = (dto.Decision ?? "").Trim();
            if (
                !string.Equals(decision, "Pass", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(decision, "Fail", StringComparison.OrdinalIgnoreCase)
            )
                throw new ArgumentException("Invalid decision");

            if (decision.Equals("Fail", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(dto.Comment))
                throw new ArgumentException("Fail reason is required.");

            // HOD Conflict Check: Proposer or Mentor cannot finalize
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null) throw new KeyNotFoundException("Thesis not found.");
            if (thesis.UserId == hodUserId) throw new UnauthorizedAccessException("You cannot finalize your own thesis proposal.");

            var hod = await _lecturerRepository.GetByEmailAsync((await _userRepository.GetByIdAsync(hodUserId))?.Email ?? "");
            if (hod != null && (thesis.MentorId1 == hod.LecturerId || thesis.MentorId2 == hod.LecturerId))
            {
                throw new UnauthorizedAccessException("Mentors cannot make final decisions for their own teams.");
            }

            await _thesisReviewRepository.UpsertHodDecisionAsync(
                thesisId,
                hodUserId,
                decision.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Pass" : "Fail",
                dto.Comment?.Trim(),
                dto.CheckedChecklistIds
            );

            await ApplyDecisionToThesisStatusAsync(
                thesisId,
                await _thesisReviewRepository.GetReviewStatusAsync(thesisId)
            );
            return await _thesisReviewRepository.GetReviewStatusAsync(thesisId);
        }

        private async Task ApplyDecisionToThesisStatusAsync(
            string thesisId,
            ThesisReviewStatusDTO status
        )
        {
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            // HOD decision is FINAL (Priority 1)
            if (status.HodDecision != null)
            {
                var isPass = string.Equals(
                    status.HodDecision.Decision,
                    "Pass",
                    StringComparison.OrdinalIgnoreCase
                );
                
                if (isPass)
                {
                    thesis.Status = thesis.TeamId.HasValue ? "Registered" : "Published";
                }
                else
                {
                    thesis.Status = "Need Update";
                }

                thesis.UpdateDate = DateTime.UtcNow;
                await _thesisRepository.UpdateThesisAsync(thesis);
                return;
            }

            // Reviewer decisions (Priority 2)
            if (string.Equals(status.OverallStatus, "Pass", StringComparison.OrdinalIgnoreCase))
            {
                thesis.Status = thesis.TeamId.HasValue ? "Registered" : "Published";
            }
            else if (
                string.Equals(status.OverallStatus, "Fail", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status.OverallStatus, "Split", StringComparison.OrdinalIgnoreCase)
            )
            {
                thesis.Status = "Need Update";
            }
            else
            {
                // Incomplete reviews
                thesis.Status = "Reviewing";
            }

            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);
        }

        private async Task<string> ResolveTimelineActorRoleAsync(User user, Thesis thesis)
        {
            var roleName = user.Role?.RoleName;
            var isLecturerRole = string.Equals(
                roleName,
                CampusConstants.Roles.Lecturer,
                StringComparison.OrdinalIgnoreCase
            );
            var isHodRole = string.Equals(
                roleName,
                CampusConstants.Roles.HOD,
                StringComparison.OrdinalIgnoreCase
            );

            // 1. Check if the user is the proposer
            if (user.UserId == thesis.UserId)
            {
                return "AUTHOR";
            }

            // 2. Check if HOD
            if (isHodRole)
            {
                return "HOD";
            }

            // 3. Check for specified Lecturer roles (Mentor or Reviewer)
            if (isLecturerRole)
            {
                var lecturer = await _lecturerRepository.GetByEmailAsync(user.Email);
                if (lecturer != null)
                {
                    if (lecturer.IsReviewer)
                        return "REVIEWER";

                    var isMentor =
                        thesis.MentorId1 == lecturer.LecturerId
                        || thesis.MentorId2 == lecturer.LecturerId;
                    if (isMentor)
                        return "MENTOR";
                }
            }

            // If we reach here, either they are a student (even if proposer) or a non-assigned lecturer.
            throw new UnauthorizedAccessException(
                "Only assigned mentor/reviewer, HOD, or a lecturer who proposed the thesis can interact with the timeline."
            );
        }

        #endregion

        #region Thesis Query and Filters

        /// <summary>
        /// Get full thesis detail including version history.
        /// </summary>
        public async Task<ThesisDTO?> GetThesisDetailAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Thesis ID cannot be empty.");

            var thesis = await _thesisRepository.GetThesisByIdWithHistoriesAsync(id);
            if (thesis == null)
                return null;

            var dto = _mapper.Map<ThesisDTO>(thesis);

            // Populate Owner Avatar
            if (!string.IsNullOrEmpty(dto.OwnerEmail))
            {
                var lecturer = await _lecturerRepository.GetByEmailAsync(dto.OwnerEmail);
                if (lecturer != null && !string.IsNullOrEmpty(lecturer.Avatar))
                {
                    dto.OwnerAvatar = lecturer.Avatar;
                }
                else
                {
                    var user = await _userRepository.GetByEmailAsync(dto.OwnerEmail);
                    dto.OwnerAvatar = user?.Avatar;
                }
            }

            var reviewStatus = await _thesisReviewRepository.GetReviewStatusAsync(id);

            dto.Reviews = reviewStatus?.Reviewers is null
                ? new List<ReviewDTO>()
                : reviewStatus
                    .Reviewers.Select(r => new ReviewDTO
                    {
                        ThesisId = id,
                        ReviewerId = r.UserId,
                        ReviewerName = r.FullName,
                        Decision = string.IsNullOrWhiteSpace(r.Decision) ? "Pending" : r.Decision,
                        Comment = r.Comment,
                        ReviewedAt = r.ReviewedAt ?? DateTime.MinValue,
                    })
                    .ToList();

            if (reviewStatus?.HodDecision != null)
            {
                dto.Reviews.Add(
                    new ReviewDTO
                    {
                        ThesisId = id,
                        ReviewerId = reviewStatus.HodDecision.HodId,
                        ReviewerName = reviewStatus.HodDecision.FullName + " (HOD)",
                        Decision = reviewStatus.HodDecision.Decision,
                        Comment = reviewStatus.HodDecision.Comment,
                        ReviewedAt = reviewStatus.HodDecision.DecidedAt,
                    }
                );
            }

            return dto;
        }

        /// <summary>
        /// Get filtered list of theses. All filters are optional.
        /// </summary>
        public async Task<IEnumerable<ThesisDTO>> GetFilteredThesesAsync(
            string? status,
            int? userId,
            int? teamId = null,
            string? searchTitle = null,
            int? semesterId = null,
            bool? isLocked = null,
            bool lecturerOnly = false,
            int? excludeUserId = null,
            string? currentUserEmail = null
        )
        {
            var user = await _userRepository.GetByEmailAsync(currentUserEmail ?? "");
            bool isHodOrAdmin =
                user != null
                && user.Role != null
                && (
                    string.Equals(
                        user.Role.RoleName,
                        CampusConstants.Roles.HOD,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || string.Equals(
                        user.Role.RoleName,
                        CampusConstants.Roles.Admin,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (semesterId == null || !isHodOrAdmin)
            {
                var currentSem = await _semesterRepository.GetCurrentSemesterAsync();
                semesterId = currentSem?.SemesterId;
            }

            var theses = await _thesisRepository.GetAllThesesFilteredAsync(
                status,
                userId,
                teamId,
                semesterId,
                isLocked,
                lecturerOnly,
                excludeUserId
            );
            var dtos = _mapper.Map<IEnumerable<ThesisDTO>>(theses);

            // Apply Reviewer restrictions
            if (!string.IsNullOrEmpty(currentUserEmail))
            {
                // 1. Conflict of Interest Check: If you are Mentor 1 or 2, you cannot see it in the review list
                dtos = dtos.Where(d =>
                    !string.Equals(d.MentorEmail1, currentUserEmail, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(d.MentorEmail2, currentUserEmail, StringComparison.OrdinalIgnoreCase)
                );

                // 2. Role-specific restrictions
                if (user != null)
                {
                    var role = user.Role?.RoleName;

                    // Lecturers (non-HOD) have stricter visibility on "On Mentor Inviting"
                    if (string.Equals(role, CampusConstants.Roles.Lecturer, StringComparison.OrdinalIgnoreCase))
                    {
                        var lecturer = await _lecturerRepository.GetByEmailAsync(currentUserEmail);
                        if (lecturer != null && lecturer.IsReviewer)
                        {
                            dtos = dtos.Where(d =>
                                !string.Equals(d.Status, "On Mentor Inviting", StringComparison.OrdinalIgnoreCase)
                                || d.UserId == user.UserId // Owner exception
                            );
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                dtos = dtos.Where(d =>
                     (d.Title != null && d.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase)) ||
                     (d.OwnerName != null && d.OwnerName.Contains(searchTitle, StringComparison.OrdinalIgnoreCase))
                 );
            }
            return dtos;
        }

        #endregion

        #region AI Review Preview

        public async Task<ThesisAIReviewPreviewDTO> GetAIReviewPreviewAsync(
            string thesisId,
            int actorUserId,
            CancellationToken cancellationToken = default
        )
        {
            if (
                _aiService is null
                || _checklistRepository is null
                || _httpClientFactory is null
                || _userAiSettingsService is null
            )
                throw new InvalidOperationException("AI review dependencies are not configured.");

            if (!_aiService.IsEnabled)
                throw new InvalidOperationException("AI review is currently disabled.");

            var actor = await _userRepository.GetByIdAsync(actorUserId);
            var isAllowed =
                actor?.Role?.RoleName == CampusConstants.Roles.HOD
                || actor?.Role?.RoleName == CampusConstants.Roles.Admin;
            if (!isAllowed)
                throw new UnauthorizedAccessException("Only HOD or Admin can use AI review.");

            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            var checklistItems = (await _checklistRepository.GetAllAsync())
                .OrderBy(x => x.ChecklistId)
                .ToList();

            if (!checklistItems.Any())
                throw new InvalidOperationException("Checklist criteria is empty.");

            var warnings = new List<string>();
            var extraction = await TryExtractThesisTextAsync(thesis.FileUrl, cancellationToken);

            if (!string.IsNullOrWhiteSpace(extraction.Warning))
                warnings.Add(extraction.Warning);

            var sourceText = extraction.Text;
            var usedMetadataFallback = extraction.UsedMetadataFallback;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                sourceText = BuildThesisMetadataContext(thesis);
                usedMetadataFallback = true;
                warnings.Add(
                    "AI review used thesis metadata because PDF text could not be extracted."
                );
            }

            if (sourceText.Length > MaxExtractedChars)
            {
                sourceText = sourceText[..MaxExtractedChars];
                warnings.Add("Thesis content was truncated to keep AI review responsive.");
            }

            var userProviderSettings =
                await _userAiSettingsService.GetEffectiveProviderSettingsAsync(
                    actorUserId,
                    cancellationToken
                );
            if (userProviderSettings is null)
                throw new InvalidOperationException(
                    "Cannot retrieve API key from Redis. Configure your AI provider key in AI Settings first."
                );

            var prompt = BuildAiReviewPrompt(thesis, checklistItems, sourceText);

            var aiRequest = new AIRequest
            {
                Messages = new[] { new AIMessage(AIMessageRole.User, prompt) },
                SystemPrompt = "You are a strict thesis evaluator. Return valid JSON only.",
                Temperature = 0.1f,
                MaxTokens = 10000,
                UseCache = false,
                UserId = actorUserId.ToString(),
                Provider = userProviderSettings.Provider,
                ProviderSettings = new AIProviderRequestSettings
                {
                    ApiKey = userProviderSettings.ApiKey,
                    Model = userProviderSettings.Model,
                    BaseUrl = userProviderSettings.BaseUrl,
                    ApiVersion = userProviderSettings.ApiVersion,
                    DeploymentName = userProviderSettings.DeploymentName,
                    TimeoutSeconds = userProviderSettings.TimeoutSeconds,
                    MaxRetries = userProviderSettings.MaxRetries,
                },
            };

            AIResponse response;
            try
            {
                response = await _aiService.ChatAsync(aiRequest, cancellationToken);
            }
            catch (AIException ex) when (IsGeminiMaxTokensError(ex))
            {
                warnings.Add(
                    "AI response hit a token limit. Retried with a larger response token budget."
                );
                response = await _aiService.ChatAsync(
                    aiRequest with
                    {
                        MaxTokens = 6000,
                    },
                    cancellationToken
                );
            }

            var parsed = ParseAiReviewContent(response.Content, checklistItems, warnings);

            return new ThesisAIReviewPreviewDTO
            {
                SuggestedDecision = parsed.SuggestedDecision,
                Feedback = parsed.Feedback,
                Checklist = parsed.Checklist,
                Warnings = warnings,
                UsedMetadataFallback = usedMetadataFallback,
                Provider = response.Provider,
                Model = response.Model,
                GeneratedAtUtc = DateTime.UtcNow,
            };
        }

        private static string BuildThesisMetadataContext(Thesis thesis)
        {
            return $"Title: {thesis.Title}\nStatus: {thesis.Status}\nShort Description: {thesis.ShortDescription}";
        }

        private static string BuildAiReviewPrompt(
            Thesis thesis,
            IReadOnlyList<Checklist> checklistItems,
            string thesisText
        )
        {
            var criteriaJson = string.Join(
                ",\n",
                checklistItems.Select(c =>
                    $"  {{ \"checklistId\": {c.ChecklistId}, \"content\": {JsonSerializer.Serialize(c.Content)} }}"
                )
            );

            return $@"Evaluate this thesis against each checklist criterion and produce a strict JSON response.

Return ONLY valid JSON with this exact shape:
{{
  ""suggestedDecision"": ""OK"" | ""Consider"",
  ""feedback"": ""string"",
  ""checklist"": [
    {{ ""checklistId"": 1, ""checked"": true, ""reason"": ""short reason"" }}
  ]
}}

Rules:
- Every checklistId from input must be present exactly once.
- Mark checked=true only if criterion is clearly satisfied by thesis content.
- feedback should be concise actionable feedback (max 900 chars).
- If evidence is weak, prefer suggestedDecision=""Consider"".
- Never include markdown, code fences, or extra text.

Thesis:
- ThesisId: {thesis.ThesisId}
- Title: {thesis.Title}
- ShortDescription: {thesis.ShortDescription}

Checklist:
[
{criteriaJson}
]

Thesis content:
{thesisText}";
        }

        private static (
            string SuggestedDecision,
            string Feedback,
            List<ThesisAIReviewChecklistItemDTO> Checklist
        ) ParseAiReviewContent(
            string raw,
            IReadOnlyList<Checklist> checklistItems,
            List<string> warnings
        )
        {
            var clean = StripCodeFence(raw).Trim();
            var checklistById = checklistItems.ToDictionary(x => x.ChecklistId, _ => false);
            var reasons = new Dictionary<int, string?>();
            var suggestedDecision = "Consider";
            var feedback = "AI could not produce detailed feedback. Please review manually.";

            try
            {
                using var document = JsonDocument.Parse(clean);
                var root = document.RootElement;

                if (root.TryGetProperty("suggestedDecision", out var decisionProp))
                    suggestedDecision = NormalizeDecision(decisionProp.GetString());

                if (
                    root.TryGetProperty("feedback", out var feedbackProp)
                    && feedbackProp.ValueKind == JsonValueKind.String
                )
                    feedback = feedbackProp.GetString() ?? feedback;

                if (
                    root.TryGetProperty("checklist", out var checklistProp)
                    && checklistProp.ValueKind == JsonValueKind.Array
                )
                {
                    foreach (var item in checklistProp.EnumerateArray())
                    {
                        if (
                            !item.TryGetProperty("checklistId", out var idProp)
                            || idProp.ValueKind != JsonValueKind.Number
                            || !idProp.TryGetInt32(out var id)
                            || !checklistById.ContainsKey(id)
                        )
                        {
                            continue;
                        }

                        var isChecked = false;
                        if (item.TryGetProperty("checked", out var checkedProp))
                        {
                            isChecked = checkedProp.ValueKind switch
                            {
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.String => string.Equals(
                                    checkedProp.GetString(),
                                    "true",
                                    StringComparison.OrdinalIgnoreCase
                                ),
                                _ => false,
                            };
                        }

                        checklistById[id] = isChecked;

                        if (
                            item.TryGetProperty("reason", out var reasonProp)
                            && reasonProp.ValueKind == JsonValueKind.String
                        )
                        {
                            reasons[id] = reasonProp.GetString();
                        }
                    }
                }
                else
                {
                    warnings.Add(
                        "AI response did not include checklist results. All criteria left unchecked."
                    );
                }
            }
            catch (JsonException)
            {
                warnings.Add("AI response was not valid JSON. Manual review is recommended.");
            }

            var normalizedChecklist = checklistItems
                .Select(c => new ThesisAIReviewChecklistItemDTO
                {
                    ChecklistId = c.ChecklistId,
                    Checked = checklistById[c.ChecklistId],
                    Reason = reasons.TryGetValue(c.ChecklistId, out var reason) ? reason : null,
                })
                .ToList();

            return (suggestedDecision, feedback, normalizedChecklist);
        }

        private static string NormalizeDecision(string? decision)
        {
            if (string.IsNullOrWhiteSpace(decision))
                return "Consider";

            var normalized = decision.Trim();
            if (
                string.Equals(normalized, "OK", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Pass", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Published", StringComparison.OrdinalIgnoreCase)
            )
            {
                return "OK";
            }

            return "Consider";
        }

        private static bool IsGeminiMaxTokensError(AIException ex)
        {
            if (ex.Code != AIErrorCode.InvalidRequest)
                return false;

            if (!ex.ProviderName.Contains("Gemini", StringComparison.OrdinalIgnoreCase))
                return false;

            return ex.Message.Contains("MAX_TOKENS", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("max tokens", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("max_tokens", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<(
            string Text,
            bool UsedMetadataFallback,
            string? Warning
        )> TryExtractThesisTextAsync(string? fileUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return (string.Empty, true, "No thesis file found. Using metadata-only AI review.");

            // Determine file type from the URL path (strip query string first)
            var urlPath = fileUrl.Contains('?') ? fileUrl[..fileUrl.IndexOf('?')] : fileUrl;
            var ext = System.IO.Path.GetExtension(urlPath).ToLowerInvariant();
            var isDocx = ext is ".docx" or ".doc";

            try
            {
                var client = _httpClientFactory!.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);

                using var response = await client.GetAsync(
                    fileUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );
                response.EnsureSuccessStatusCode();

                // Also check Content-Type if extension was not conclusive
                if (!isDocx)
                {
                    var contentType =
                        response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                    isDocx =
                        contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)
                        || contentType.Contains("msword", StringComparison.OrdinalIgnoreCase);
                }

                await using var responseStream = await response.Content.ReadAsStreamAsync(
                    cancellationToken
                );
                using var memory = new MemoryStream();
                await responseStream.CopyToAsync(memory, cancellationToken);
                memory.Position = 0;

                string rawText;
                if (isDocx)
                {
                    rawText = ExtractDocxText(memory);
                }
                else
                {
                    using var document = PdfDocument.Open(memory);
                    var sb = new StringBuilder();

                    foreach (var page in document.GetPages())
                    {
                        var pageText = ExtractPageTextWithTableHeuristics(page);
                        if (string.IsNullOrWhiteSpace(pageText))
                            pageText = page.Text;

                        sb.AppendLine(pageText);
                        if (sb.Length >= MaxExtractedChars * 2)
                            break;
                    }

                    rawText = sb.ToString();
                }

                var extracted = NormalizeWhitespace(rawText);
                if (string.IsNullOrWhiteSpace(extracted))
                    return (
                        string.Empty,
                        true,
                        isDocx
                            ? "Thesis Word document text extraction returned empty content."
                            : "Thesis PDF text extraction returned empty content."
                    );

                return (extracted, false, null);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Failed to extract thesis file text from URL for thesis AI review."
                );
                return (
                    string.Empty,
                    true,
                    isDocx
                        ? "Could not extract text from thesis Word document. Using metadata-only AI review."
                        : "Could not extract text from thesis PDF. Using metadata-only AI review."
                );
            }
        }

        private static string ExtractDocxText(MemoryStream stream)
        {
            using var wordDoc = WordprocessingDocument.Open(stream, isEditable: false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body is null)
                return string.Empty;

            var sb = new StringBuilder();

            foreach (var element in body.ChildElements)
            {
                if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
                {
                    // Render table rows as pipe-delimited lines
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>()
                            .Select(tc => tc.InnerText.Trim())
                            .ToList();

                        if (cells.Count >= 2)
                            sb.AppendLine(string.Join(" | ", cells));
                        else if (cells.Count == 1 && !string.IsNullOrWhiteSpace(cells[0]))
                            sb.AppendLine(cells[0]);
                    }
                }
                else if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph para)
                {
                    var text = para.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                        sb.AppendLine(text);
                }
            }

            return sb.ToString();
        }

        private static string ExtractPageTextWithTableHeuristics(Page page)
        {
            var letters = page
                .Letters.Where(l => !string.IsNullOrEmpty(l.Value))
                .Where(l => !string.IsNullOrWhiteSpace(l.Value) || l.GlyphRectangle.Width > 0)
                .ToList();

            if (letters.Count < 20)
                return page.Text;

            var heights = letters
                .Select(l => Math.Abs(l.GlyphRectangle.Top - l.GlyphRectangle.Bottom))
                .Where(h => h > 0)
                .OrderBy(h => h)
                .ToList();
            var medianHeight = heights.Count == 0 ? 8d : heights[heights.Count / 2];
            var rowTolerance = Math.Max(2d, medianHeight * 0.6d);

            var lineBuckets = new List<List<Letter>>();
            var lineCenters = new List<double>();

            foreach (
                var letter in letters
                    .OrderByDescending(l => (l.GlyphRectangle.Top + l.GlyphRectangle.Bottom) / 2d)
                    .ThenBy(l => l.GlyphRectangle.Left)
            )
            {
                var centerY = (letter.GlyphRectangle.Top + letter.GlyphRectangle.Bottom) / 2d;
                var assigned = false;

                for (var i = 0; i < lineCenters.Count; i++)
                {
                    if (Math.Abs(lineCenters[i] - centerY) <= rowTolerance)
                    {
                        lineBuckets[i].Add(letter);
                        lineCenters[i] = (lineCenters[i] + centerY) / 2d;
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    lineBuckets.Add(new List<Letter> { letter });
                    lineCenters.Add(centerY);
                }
            }

            var rows = lineBuckets
                .Select(bucket =>
                {
                    var ordered = bucket.OrderBy(l => l.GlyphRectangle.Left).ToList();
                    return BuildRowText(ordered);
                })
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();

            if (!rows.Any())
                return page.Text;

            return string.Join('\n', rows);
        }

        private static string BuildRowText(IReadOnlyList<Letter> letters)
        {
            if (letters.Count == 0)
                return string.Empty;

            var gaps = new List<double>();
            for (var i = 1; i < letters.Count; i++)
            {
                var gap = letters[i].GlyphRectangle.Left - letters[i - 1].GlyphRectangle.Right;
                if (gap > 0)
                    gaps.Add(gap);
            }

            var positiveGaps = gaps.OrderBy(g => g).ToList();
            var medianGap = positiveGaps.Count == 0 ? 2d : positiveGaps[positiveGaps.Count / 2];
            var tableSplitGap = Math.Max(12d, medianGap * 2.2d);
            var wordSplitGap = Math.Max(1.5d, medianGap * 0.9d);

            var cells = new List<string>();
            var currentCell = new StringBuilder();

            currentCell.Append(letters[0].Value);
            for (var i = 1; i < letters.Count; i++)
            {
                var gap = letters[i].GlyphRectangle.Left - letters[i - 1].GlyphRectangle.Right;

                if (gap >= tableSplitGap)
                {
                    cells.Add(currentCell.ToString().Trim());
                    currentCell.Clear();
                    currentCell.Append(letters[i].Value);
                    continue;
                }

                if (gap >= wordSplitGap && currentCell.Length > 0)
                {
                    currentCell.Append(' ');
                }

                currentCell.Append(letters[i].Value);
            }

            cells.Add(currentCell.ToString().Trim());
            cells = cells.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();

            if (cells.Count >= 3)
            {
                return string.Join(" | ", cells);
            }

            return cells.Count == 0 ? string.Empty : cells[0];
        }

        private static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalizedLines = text.Replace("\r", string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                    string.Join(
                        " ",
                        line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    )
                )
                .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join('\n', normalizedLines);
        }

        private static string StripCodeFence(string input)
        {
            var trimmed = input.Trim();
            if (!trimmed.StartsWith("```", StringComparison.Ordinal))
                return trimmed;

            var firstLineEnd = trimmed.IndexOf('\n');
            if (firstLineEnd < 0)
                return trimmed;

            var content = trimmed[(firstLineEnd + 1)..];
            var fenceEnd = content.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd >= 0)
                content = content[..fenceEnd];

            return content.Trim();
        }

        #endregion

        #region Locking and Assignment

        /// <summary>
        /// Toggle the locked state of a lecturer-proposed thesis.
        /// Only the owning Lecturer can lock/unlock their own thesis.
        /// </summary>
        public async Task<ThesisDTO> ToggleThesisLockAsync(string thesisId, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (
                user.Role?.RoleName != CampusConstants.Roles.Lecturer
                && user.Role?.RoleName != CampusConstants.Roles.HOD
            )
                throw new UnauthorizedAccessException(
                    "Only lecturers or HODs can lock or unlock a thesis."
                );

            var thesis = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            if (thesis.UserId != user.UserId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to lock/unlock this thesis."
                );

            if (thesis.Status != "Published")
                throw new InvalidOperationException(
                    "Registration can only be toggled for 'Published' theses."
                );

            thesis.IsLocked = !thesis.IsLocked;
            thesis.UpdateDate = DateTime.UtcNow;

            await _thesisRepository.UpdateThesisAsync(thesis);

            return _mapper.Map<ThesisDTO>(thesis);
        }

        // ─── F105: Force Assign Thesis ───────────────────────────────────────────

        public async Task<ThesisDTO> ForceAssignThesisAsync(
            string thesisId,
            int teamId,
            int hodUserId
        )
        {
            // 1. Validate HOD role
            var hodUser = await _userRepository.GetByIdAsync(hodUserId);
            if (hodUser == null || hodUser.Role?.RoleName != CampusConstants.Roles.HOD)
                throw new UnauthorizedAccessException(
                    "Only Head of Department can force-assign theses."
                );

            // 2. Get thesis
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException($"Thesis '{thesisId}' not found.");

            // 3. Thesis must be Published
            if (thesis.Status != "Published")
                throw new InvalidOperationException(
                    $"Thesis must be 'Published' to force-assign. Current status: '{thesis.Status}'."
                );

            // 4. Thesis must not already be assigned
            if (thesis.TeamId != null)
                throw new InvalidOperationException("This thesis is already assigned to a team.");

            // 5. Get team
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
                throw new KeyNotFoundException($"Team {teamId} not found.");

            // 6. Team must not already have a thesis in this semester
            if (thesis.SemesterId.HasValue)
            {
                var existingThesis = await _thesisRepository.GetApprovedThesisByLeaderIdAsync(
                    team.LeaderId,
                    thesis.SemesterId
                );
                if (existingThesis != null)
                    throw new InvalidOperationException(
                        $"Team '{team.TeamName}' already has thesis '{existingThesis.Title}' in this semester."
                    );
            }

            // 7. Assign
            thesis.TeamId = teamId;
            thesis.Status = "Registered";
            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);

            // Reload for mapper
            var updated = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            return _mapper.Map<ThesisDTO>(updated!);
        }

        #endregion
    }
}
