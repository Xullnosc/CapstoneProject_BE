using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Services.Helpers;

namespace Services
{
    public class MentorInvitationService : IMentorInvitationService
    {
        private readonly ITeamInvitationRepository _invitationRepo;
        private readonly ITeamRepository _teamRepo;
        private readonly IUserRepository _userRepo;
        private readonly IThesisRepository _thesisRepo;
        private readonly IWhitelistRepository _whitelistRepo;
        private readonly ISemesterRepository _semesterRepo;
        private readonly ILecturerRepository _lecturerRepo;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IRedisService _redisService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MentorInvitationService> _logger;

        // Cache key helpers
        private static string MentorInvitationsKey(int mentorId) => $"mentor-invitations:{mentorId}";
        private static string MentorActiveCountKey(int mentorId) => $"mentor-active-count:{mentorId}";
        private static readonly TimeSpan InvitationsTTL = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ActiveCountTTL = TimeSpan.FromMinutes(2);

        public MentorInvitationService(
            ITeamInvitationRepository invitationRepo,
            ITeamRepository teamRepo,
            IUserRepository userRepo,
            IThesisRepository thesisRepo,
            IWhitelistRepository whitelistRepo,
            ISemesterRepository semesterRepo,
            ILecturerRepository lecturerRepo,
            IEmailService emailService,
            IConfiguration configuration,
            IRedisService redisService,
            INotificationService notificationService,
            ILogger<MentorInvitationService> logger)
        {
            _invitationRepo = invitationRepo;
            _teamRepo = teamRepo;
            _userRepo = userRepo;
            _thesisRepo = thesisRepo;
            _whitelistRepo = whitelistRepo;
            _semesterRepo = semesterRepo;
            _lecturerRepo = lecturerRepo;
            _emailService = emailService;
            _configuration = configuration;
            _redisService = redisService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<PagedResult<MentorInvitationDTO>> GetMentorInvitationsAsync(int mentorId, int pageIndex, int pageSize)
        {
            // Cache only first page with default page size
            if (pageIndex == 1 && pageSize == 10)
            {
                var cached = await _redisService.GetObjectAsync<PagedResult<MentorInvitationDTO>>(MentorInvitationsKey(mentorId));
                if (cached != null) return cached;
            }

            var pagedResult = await _invitationRepo.GetPendingMentorInvitationsByMentorIdAsync(mentorId, pageIndex, pageSize);
            
            // Bulk fetch theses for teams in invitations
            var teamIds = pagedResult.Items.Select(i => i.TeamId).Distinct();
            var semester = await _semesterRepo.GetCurrentSemesterAsync();
            var theses = await _thesisRepo.GetThesesByOwnerOrTeamAsync(new List<int>(), teamIds, semester?.SemesterId);
            var thesisMap = theses.Where(t => t.TeamId.HasValue).ToDictionary(t => t.TeamId!.Value);

            var dtos = pagedResult.Items.Select(i => MapToDTO(i, thesisMap.GetValueOrDefault(i.TeamId))).ToList();
            var result = new PagedResult<MentorInvitationDTO>(dtos, pagedResult.TotalCount, pagedResult.PageIndex, pagedResult.PageSize);

            if (pageIndex == 1 && pageSize == 10)
                await _redisService.SetObjectAsync(MentorInvitationsKey(mentorId), result, InvitationsTTL);

            return result;
        }

        public async Task<PagedResult<MentorInvitationDTO>> GetTeamMentorInvitationsAsync(int teamId, int leaderId, int pageIndex, int pageSize)
        {
            var team = await _teamRepo.GetByIdAsync(teamId);
            if (team == null)
            {
                throw new Exception("Team not found.");
            }
            if (team.LeaderId != leaderId)
            {
                throw new UnauthorizedAccessException("Only the team leader can view mentor invitations.");
            }

            var pagedResult = await _invitationRepo.GetMentorInvitationsByTeamAsync(teamId, pageIndex, pageSize);
            
            // Fetch thesis for this team once
            var thesis = await _thesisRepo.GetThesisForInvitationAsync(team.LeaderId, team.SemesterId);

            var dtos = pagedResult.Items.Select(i => MapToDTO(i, thesis)).ToList();
            return new PagedResult<MentorInvitationDTO>(dtos, pagedResult.TotalCount, pagedResult.PageIndex, pagedResult.PageSize);
        }

        public async Task<MentorInvitationDTO> SendMentorInvitationAsync(int teamId, int leaderId, string mentorEmail)
        {
            if (string.IsNullOrWhiteSpace(mentorEmail))
            {
                throw new ArgumentException("Mentor email is required.");
            }

            var team = await _teamRepo.GetByIdAsync(teamId);
            if (team == null) throw new Exception("Team not found.");
            if (team.LeaderId != leaderId) throw new UnauthorizedAccessException("Only team leader can send mentor invitations.");
            if (team.MentorId != null && team.MentorId2 != null) throw new Exception("Team already has the maximum of 2 mentors.");

            // Check if current semester exists
            var currentSemester = await _semesterRepo.GetCurrentSemesterAsync();
            if (currentSemester == null) throw new Exception("Current semester not found.");

            // [LIFECYCLE GUARD] Chỉ cho phép mời Mentor khi kỳ học ở trạng thái Open
            if (!CampusConstants.SemesterStatus.IsOpenStage(currentSemester.Status))
            {
                throw new InvalidOperationException($"Kỳ học đang ở trạng thái '{currentSemester.Status}'. Chỉ có thể mời Mentor khi kỳ học đang mở (Open).");
            }

            // Check if thesis is in "On Mentor Inviting" status for the current semester
            var thesis = await _thesisRepo.GetThesisForInvitationAsync(leaderId, currentSemester.SemesterId);
            if (thesis == null)
            {
                throw new Exception("Your team must have a thesis before inviting a mentor.");
            }

            // 1. Try to find in Global Lecturer Pool (Priority)
            var globalLecturer = await _lecturerRepo.GetByEmailAsync(mentorEmail);
            
            // 2. Try to find in Whitelist (Fallback/Legacy)
            var whitelistEntry = await _whitelistRepo.GetByEmailAsync(mentorEmail);

            if (globalLecturer == null && whitelistEntry == null)
            {
                throw new Exception("Mentor is not found in the global lecturer pool or semester whitelist.");
            }

            // If it's a student in whitelist, we reject if we specifically want a mentor (lecturer/HOD role)
            // HOD = 1, Lecturer = 2
            if (globalLecturer == null && whitelistEntry != null && whitelistEntry.RoleId != 1 && whitelistEntry.RoleId != 2)
            {
                throw new Exception("Invited user is not a lecturer or HOD.");
            }

            var mentor = await _userRepo.GetByEmailAsync(mentorEmail);
            
            // If mentor user record doesn't exist, create Shell Account from either Pool or Whitelist
            if (mentor == null)
            {
                mentor = new User
                {
                    Email = globalLecturer?.Email ?? whitelistEntry!.Email,
                    FullName = globalLecturer?.FullName ?? whitelistEntry?.FullName ?? "Lecturer",
                    Avatar = globalLecturer?.Avatar ?? whitelistEntry?.Avatar ?? "",
                    RoleId = 2, // Default to Lecturer role id
                    CampusId = globalLecturer?.CampusId ?? whitelistEntry?.CampusId,
                    IsAuthorized = true,
                    CreatedAt = DateTime.UtcNow
                };
                mentor = await _userRepo.AddAsync(mentor);
            }
            else
            {
                // Check if the user is actually a lecturer or HOD
                if (mentor.Role?.RoleName != CampusConstants.Roles.Lecturer && 
                    mentor.Role?.RoleName != CampusConstants.Roles.HOD &&
                    mentor.RoleId != 1 && mentor.RoleId != 2) 
                    throw new Exception("Invited user is not a lecturer or HOD.");
            }
            
            if (mentor.UserId == leaderId) throw new Exception("You cannot invite yourself.");
            
            if (team.MentorId == mentor.UserId || team.MentorId2 == mentor.UserId)
            {
                throw new Exception("This user is already a mentor of your team.");
            }

            var existingInvite = await _invitationRepo.GetByTeamAndMentorAsync(teamId, mentor.UserId);
            if (existingInvite != null && existingInvite.Status == CampusConstants.InvitationStatus.Pending)
            {
                throw new Exception("An invitation has already been sent to this mentor.");
            }

            var invitation = new Teaminvitation
            {
                TeamId = teamId,
                StudentId = mentor.UserId, // Using StudentId column for Mentor UserId (Legacy schema)
                InvitedBy = leaderId,
                Type = CampusConstants.InvitationType.Mentor,
                Status = CampusConstants.InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _invitationRepo.CreateAsync(invitation);

            // Send Email Notification
            try
            {
                var inviter = await _userRepo.GetByIdAsync(leaderId);
                var inviterName = inviter?.FullName ?? "A Team Leader";
                var teamName = team.TeamName;
                var mentorName = mentor.FullName;

                // Dynamic Frontend URL
                string frontendUrl = "http://localhost:5173"; // Default
                var allowedOrigins = _configuration["AllowedOrigins"];
                if (!string.IsNullOrEmpty(allowedOrigins))
                {
                    var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (origins.Length > 0)
                    {
                        frontendUrl = origins[0].Trim();
                    }
                }

                string link = $"{frontendUrl}/mentor-invitations";
                string subject = $"[FCTMS] Mentor Invitation for Team {teamName}";

                string htmlContent = EmailTemplateConstants.MentorInvitationTemplate
                    .Replace("{MentorName}", mentorName)
                    .Replace("{TeamName}", teamName)
                    .Replace("{InviterName}", inviterName)
                    .Replace("{InvitationLink}", link)
                    .Replace("{CurrentYear}", DateTime.Now.Year.ToString());

                await _emailService.SendEmailAsync(mentor.Email, subject, htmlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MentorInvitationService] Failed to send email: {ex.Message}");
            }
            
            // Reload with relations for mapping
            var loadedInvitation = await _invitationRepo.GetByIdAsync(created.InvitationId);

            await TryCreateNotificationAsync(
                mentor.UserId,
                NotificationType.MentorChange.ToString(),
                "New mentor invitation",
                $"Team {team.TeamName} invited you to mentor them.",
                "Team",
                team.TeamId);

            return MapToDTO(loadedInvitation!, thesis);
        }

        public async Task AcceptMentorInvitationAsync(int invitationId, int mentorId)
        {
            var invitation = await _invitationRepo.GetByIdAsync(invitationId);
            if (invitation == null || invitation.Type != CampusConstants.InvitationType.Mentor || invitation.Status != CampusConstants.InvitationStatus.Pending)
            {
                throw new Exception("Invitation not found, already responded, or is not a mentor invitation.");
            }

            if (invitation.StudentId != mentorId)
            {
                throw new UnauthorizedAccessException("You can only accept your own invitations.");
            }

            var team = await _teamRepo.GetByIdAsync(invitation.TeamId);
            if (team == null) throw new Exception("Team not found.");
            
            if (team.MentorId != null && team.MentorId2 != null)
            {
                throw new Exception("This team already has the maximum of 2 mentors.");
            }

            if (team.MentorId == mentorId || team.MentorId2 == mentorId)
            {
                throw new Exception("You are already a mentor of this team.");
            }

            int activeTeamCount = await _invitationRepo.GetMentorActiveTeamCountAsync(mentorId, team.SemesterId);
            if (activeTeamCount >= 4)
            {
                throw new Exception("You have reached the maximum limit of 4 teams for this semester.");
            }

            if (team.MentorId == null)
            {
                team.MentorId = mentorId;
            }
            else
            {
                team.MentorId2 = mentorId;
            }
            await _teamRepo.UpdateAsync(team);
            await _invitationRepo.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Accepted);

            // AUTO-TRANSITION Thesis Status
            // If student proposed a thesis and it's waiting for a mentor, transition to Reviewing
            try
            {
                var thesis = await _thesisRepo.GetThesisForInvitationAsync(team.LeaderId, team.SemesterId);
                if (thesis != null && thesis.Status == "On Mentor Inviting")
                {
                    thesis.Status = "Reviewing";
                    thesis.UpdateDate = DateTime.UtcNow;
                    await _thesisRepo.UpdateThesisAsync(thesis);
                    _logger.LogInformation("Thesis {ThesisId} transitioned to 'Reviewing' after mentor {MentorId} accepted invitation for team {TeamId}.", 
                        thesis.ThesisId, mentorId, team.TeamId);
                }
            }
            catch (Exception ex)
            {
                // Non-blocking error
                _logger.LogError(ex, "Failed to auto-transition thesis status for team {TeamId} after mentor acceptance.", team.TeamId);
            }

            // Invalidate cache after accepting
            await _redisService.DeleteValueAsync(MentorInvitationsKey(mentorId));
            await _redisService.DeleteValueAsync(MentorActiveCountKey(mentorId));

            await TryCreateNotificationAsync(
                invitation.InvitedBy,
                NotificationType.MentorChange.ToString(),
                "Mentor invitation accepted",
                $"{invitation.Student?.FullName ?? "A mentor"} accepted the mentor invitation for team {team.TeamName}.",
                "Team",
                team.TeamId);
        }

        public async Task DeclineMentorInvitationAsync(int invitationId, int mentorId)
        {
            var invitation = await _invitationRepo.GetByIdAsync(invitationId);
            if (invitation == null || invitation.Type != CampusConstants.InvitationType.Mentor || invitation.Status != CampusConstants.InvitationStatus.Pending)
            {
                throw new Exception("Invitation not found, already responded, or is not a mentor invitation.");
            }

            if (invitation.StudentId != mentorId)
            {
                throw new UnauthorizedAccessException("You can only decline your own invitations.");
            }

            await _invitationRepo.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Declined);

            // Invalidate invitation cache after declining
            await _redisService.DeleteValueAsync(MentorInvitationsKey(mentorId));

            var team = await _teamRepo.GetByIdAsync(invitation.TeamId);
            await TryCreateNotificationAsync(
                invitation.InvitedBy,
                NotificationType.MentorChange.ToString(),
                "Mentor invitation declined",
                $"{invitation.Student?.FullName ?? "A mentor"} declined the mentor invitation{(team == null ? string.Empty : $" for team {team.TeamName}")}.",
                team == null ? null : "Team",
                team?.TeamId);
        }

        public async Task CancelMentorInvitationAsync(int invitationId, int leaderId)
        {
            var invitation = await _invitationRepo.GetByIdAsync(invitationId);
            if (invitation == null || invitation.Status != CampusConstants.InvitationStatus.Pending)
            {
                throw new Exception("Invitation not found or already responded.");
            }

            var team = await _teamRepo.GetByIdAsync(invitation.TeamId);
            if (team == null || team.LeaderId != leaderId)
            {
                throw new UnauthorizedAccessException("Only the team leader who sent the invitation can cancel it.");
            }

            await _invitationRepo.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Cancelled);
        }

        public async Task<int> GetMentorActiveTeamCountAsync(int mentorId)
        {
            var cached = await _redisService.GetObjectAsync<int?>(MentorActiveCountKey(mentorId));
            if (cached.HasValue) return cached.Value;

            var currentSemester = await _semesterRepo.GetCurrentSemesterAsync();
            if (currentSemester == null) return 0;
            var count = await _invitationRepo.GetMentorActiveTeamCountAsync(mentorId, currentSemester.SemesterId);

            await _redisService.SetObjectAsync(MentorActiveCountKey(mentorId), (int?)count, ActiveCountTTL);
            return count;
        }

        private MentorInvitationDTO MapToDTO(Teaminvitation entity, Thesis? thesis = null)
        {
            var dto = new MentorInvitationDTO
            {
                InvitationId = entity.InvitationId,
                TeamId = entity.TeamId,
                TeamName = entity.Team?.TeamName ?? string.Empty,
                TeamCode = DisplayHelper.FormatTeamCode(entity.Team?.TeamCode),
                MentorId = entity.StudentId,
                MentorEmail = entity.Student?.Email ?? string.Empty,
                MentorName = entity.Student?.FullName ?? string.Empty,
                InvitedById = entity.InvitedBy,
                InvitedByName = entity.InvitedByNavigation?.FullName ?? string.Empty,
                InvitedByEmail = entity.InvitedByNavigation?.Email ?? string.Empty,
                Type = entity.Type,
                Status = entity.Status ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                RespondedAt = entity.RespondedAt
            };

            if (thesis != null)
            {
                dto.ThesisId = thesis.ThesisId;
                dto.ThesisTitle = thesis.Title;
                dto.ThesisStatus = thesis.Status;
            }

            return dto;
        }

        private async Task TryCreateNotificationAsync(int userId, string type, string title, string message, string? relatedEntityType, int? relatedEntityId)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    userId,
                    type,
                    title,
                    message,
                    relatedEntityType,
                    relatedEntityId,
                    sendEmail: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create mentor invitation notification. UserId: {UserId}, Type: {Type}", userId, type);
            }
        }
    }
}
