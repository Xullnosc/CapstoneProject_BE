using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using Services.Helpers;

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

        public ThesisService(
            IThesisRepository thesisRepository,
            IThesisReviewRepository thesisReviewRepository,
            ITeamRepository teamRepository,
            IUserRepository userRepository,
            ICloudinaryHelper cloudinaryHelper,
            ISemesterRepository semesterRepository,
            ILecturerRepository lecturerRepository,
            ITeamInvitationRepository teamInvitationRepository,
            IMapper mapper
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
        }

        // ─── Existing (not modified) ─────────────────────────────────────────────

        public async Task<Thesis> ProposeThesisAsync(ProposeThesisDTO req, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new Exception("User not found");

            // Prevent multiple theses per leader, except for Lecturers.
            // Allow re-proposing if all previous theses are Cancelled or Rejected.
            var existingTheses = await _thesisRepository.GetThesesByUserIdAsync(user.UserId);
            var hasActiveThesis = existingTheses.Any(t =>
                t.Status != "Cancelled" && t.Status != "Rejected"
            );
            if (hasActiveThesis && user.Role?.RoleName != CampusConstants.Roles.Lecturer)
            {
                throw new InvalidOperationException(
                    "You have already proposed a thesis. You cannot propose more than one."
                );
            }

            // Students must be the team leader and the team must have at least 4 members.
            if (user.Role?.RoleName != CampusConstants.Roles.Lecturer)
            {
                var team = await _teamRepository.GetActiveTeamByStudentIdAsync(user.UserId);
                if (team == null)
                    throw new InvalidOperationException(
                        "You must be in an active team to propose a thesis."
                    );

                if (team.LeaderId != user.UserId)
                    throw new InvalidOperationException(
                        "Only the team leader can propose a thesis."
                    );

                if (team.Teammembers.Count < 4)
                    throw new InvalidOperationException(
                        $"Your team must have at least 4 members to propose a thesis. Current members: {team.Teammembers.Count}."
                    );
            }

            string? fileUrl = null;
            if (req.File != null)
                fileUrl = await _cloudinaryHelper.UploadFileAsync(req.File);

            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();

            var thesis = new Thesis
            {
                ThesisId = Guid.NewGuid().ToString(),
                Title = string.IsNullOrWhiteSpace(req.Title)
                    ? (
                        req.File != null
                            ? System.IO.Path.GetFileNameWithoutExtension(req.File.FileName)
                            : "Untitled"
                    )
                    : req.Title.Trim(),
                ShortDescription = req.ShortDescription,
                UserId = user.UserId,
                FileUrl = fileUrl,
                Status =
                    user.Role?.RoleName == CampusConstants.Roles.Lecturer
                        ? "Reviewing"
                        : "On Mentor Inviting",
                SemesterId = currentSemester?.SemesterId,
                UpDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow,
            };

            // Set TeamId based on role (Lecturers don't have teams)
            if (user.Role?.RoleName != CampusConstants.Roles.Lecturer)
            {
                var team = await _teamRepository.GetActiveTeamByStudentIdAsync(user.UserId);
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

        // ─── Phase 02: New Methods ───────────────────────────────────────────────

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

            // Only the owner can update
            if (thesis.UserId != user.UserId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to update this thesis."
                );

            // Upload new file to Cloudinary (if provided)
            if (req.File != null)
            {
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
                if (thesis.Status == "Need Update" || thesis.Status == "Reviewing")
                {
                    thesis.Status = "Updated";
                }
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

            if (user.Role?.RoleName == CampusConstants.Roles.Lecturer)
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
                    t.Title != null
                    && t.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase)
                );
            }

            return _mapper.Map<IEnumerable<ThesisDTO>>(theses);
        }

        // ─── Review workflow ────────────────────────────────────────────────────

        public Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId) =>
            _thesisReviewRepository.GetReviewStatusAsync(thesisId);

        public Task<List<ThesisReviewTimelineEventDTO>> GetReviewTimelineAsync(string thesisId) =>
            _thesisReviewRepository.GetTimelineAsync(thesisId);

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
            if (user == null || user.Role?.RoleName != CampusConstants.Roles.Lecturer)
                throw new UnauthorizedAccessException("Only reviewers can submit reviews.");

            var lecturer = await _lecturerRepository.GetByEmailAsync(user.Email);
            if (lecturer == null || !lecturer.IsReviewer)
                throw new UnauthorizedAccessException("You do not have reviewer permissions.");

            // 2. Ensure thesis is in Reviewing state
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            // Allow submission if it's 'On Mentor Inviting' (meaning it's just started) or 'Reviewing'
            if (thesis.Status != "Reviewing" && thesis.Status != "On Mentor Inviting")
                throw new InvalidOperationException("This thesis is not in the reviewing phase.");

            // AUTO-ASSIGN Proposer if they are a Reviewer and not already assigned
            var proposer = await _userRepository.GetByIdAsync(thesis.UserId);
            if (proposer != null && proposer.Role?.RoleName == CampusConstants.Roles.Lecturer)
            {
                var proposerLecturer = await _lecturerRepository.GetByEmailAsync(proposer.Email);
                // Only auto-pass if the proposer is flagged as a Reviewer (they are a reviewer themselves)
                if (proposerLecturer != null && proposerLecturer.IsReviewer)
                {
                    var currentReviewStatus = await _thesisReviewRepository.GetReviewStatusAsync(
                        thesisId
                    );
                    if (!currentReviewStatus.Reviewers.Any(r => r.UserId == proposer.UserId))
                    {
                        await _thesisReviewRepository.UpsertReviewerReviewAsync(
                            thesisId,
                            proposer.UserId,
                            "Pass",
                            "Automatically approved (Self-proposed by Reviewer)",
                            null
                        );
                    }
                }
            }

            string? reviewFileUrl = null;
            if (dto.ReviewFile != null)
            {
                reviewFileUrl = await _cloudinaryHelper.UploadFileAsync(dto.ReviewFile);
            }

            await _thesisReviewRepository.UpsertReviewerReviewAsync(
                thesisId,
                reviewerUserId,
                decision.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Pass" : "Fail",
                dto.Comment?.Trim(),
                reviewFileUrl
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

            if (
                string.Equals(decision, "Fail", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(dto.Comment)
            )
                throw new ArgumentException("Fail reason is required.");

            await _thesisReviewRepository.UpsertHodDecisionAsync(
                thesisId,
                hodUserId,
                decision.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Pass" : "Fail",
                dto.Comment?.Trim()
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
                thesis.Status = string.Equals(
                    status.HodDecision.Decision,
                    "Pass",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Published"
                    : "Need Update";
                thesis.UpdateDate = DateTime.UtcNow;
                await _thesisRepository.UpdateThesisAsync(thesis);
                return;
            }

            // Reviewer decisions (Priority 2)
            if (string.Equals(status.OverallStatus, "Pass", StringComparison.OrdinalIgnoreCase))
            {
                thesis.Status = "Published";
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
            if (
                string.Equals(
                    roleName,
                    CampusConstants.Roles.HOD,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "HOD";
            }

            if (
                !string.Equals(
                    roleName,
                    CampusConstants.Roles.Lecturer,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                throw new UnauthorizedAccessException(
                    "Only HOD, reviewer, or mentor can interact with thesis timeline."
                );
            }

            var lecturer = await _lecturerRepository.GetByEmailAsync(user.Email);
            if (lecturer == null)
            {
                throw new UnauthorizedAccessException("Lecturer profile not found.");
            }

            if (lecturer.IsReviewer)
            {
                return "REVIEWER";
            }

            var isMentor =
                thesis.MentorId1 == lecturer.LecturerId || thesis.MentorId2 == lecturer.LecturerId;
            if (isMentor)
            {
                return "MENTOR";
            }

            throw new UnauthorizedAccessException(
                "Only assigned mentor/reviewer or HOD can interact with thesis timeline."
            );
        }

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
            var reviewStatus = await _thesisReviewRepository.GetReviewStatusAsync(id);
            dto.Reviews = reviewStatus
                .Reviewers.Select(r => new ReviewDTO
                {
                    ThesisId = id,
                    ReviewerId = r.UserId,
                    ReviewerName = r.FullName,
                    Decision = string.IsNullOrWhiteSpace(r.Decision) ? "Pending" : r.Decision,
                    Comment = r.Comment,
                    FileUrl = r.FileUrl,
                    ReviewedAt = r.ReviewedAt ?? DateTime.MinValue,
                })
                .ToList();

            return dto;
        }

        /// <summary>
        /// Get filtered list of theses. All filters are optional.
        /// </summary>
        public async Task<IEnumerable<ThesisDTO>> GetFilteredThesesAsync(
            string? status,
            int? userId,
            string? searchTitle = null,
            int? semesterId = null,
            bool? isLocked = null,
            bool lecturerOnly = false,
            int? excludeUserId = null
        )
        {
            var theses = await _thesisRepository.GetAllThesesFilteredAsync(
                status,
                userId,
                semesterId,
                isLocked,
                lecturerOnly,
                excludeUserId
            );
            var dtos = _mapper.Map<IEnumerable<ThesisDTO>>(theses);
            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                dtos = dtos.Where(d =>
                    d.Title != null
                    && d.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase)
                );
            }
            return dtos;
        }

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
    }
}
