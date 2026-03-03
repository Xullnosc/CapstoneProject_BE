using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.Extensions.Configuration;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IRedisService _redisService;

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
            IEmailService emailService,
            IConfiguration configuration,
            IRedisService redisService)
        {
            _invitationRepo = invitationRepo;
            _teamRepo = teamRepo;
            _userRepo = userRepo;
            _thesisRepo = thesisRepo;
            _whitelistRepo = whitelistRepo;
            _semesterRepo = semesterRepo;
            _emailService = emailService;
            _configuration = configuration;
            _redisService = redisService;
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
            var dtos = pagedResult.Items.Select(MapToDTO).ToList();
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
            var dtos = pagedResult.Items.Select(MapToDTO).ToList();
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
            if (team.MentorId != null) throw new Exception("Team already has a mentor.");

            // Check if thesis is Approved or Published
            var thesis = await _thesisRepo.GetApprovedThesisByLeaderIdAsync(leaderId);
            if (thesis == null)
            {
                throw new Exception("Your team must have an 'Approved' or 'Published' thesis before inviting a mentor.");
            }

            // Check if mentor is in Whitelist for the current semester
            var currentSemester = await _semesterRepo.GetCurrentSemesterAsync();
            if (currentSemester == null) throw new Exception("Current semester not found.");

            var whitelistEntry = await _whitelistRepo.GetByEmailAsync(mentorEmail);
            if (whitelistEntry == null || whitelistEntry.SemesterId != currentSemester.SemesterId)
            {
                throw new Exception("Mentor is not in the whitelist for the current semester.");
            }

            var mentor = await _userRepo.GetByEmailAsync(mentorEmail);
            
            // If mentor doesn't exist, create Shell Account
            if (mentor == null)
            {
                mentor = new User
                {
                    Email = whitelistEntry.Email,
                    FullName = whitelistEntry.FullName ?? "Lecturer",
                    RoleId = whitelistEntry.RoleId ?? 2, // Default to Lecturer role id if not set (2)
                    Campus = whitelistEntry.Campus,
                    IsAuthorized = true,
                    CreatedAt = DateTime.UtcNow
                };
                mentor = await _userRepo.AddAsync(mentor);
            }
            else
            {
                if (mentor.Role?.RoleName != CampusConstants.Roles.Lecturer && whitelistEntry.RoleId != 2) 
                    throw new Exception("Invited user is not a lecturer.");
            }
            
            if (mentor.UserId == leaderId) throw new Exception("You cannot invite yourself.");

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

                string link = $"{frontendUrl}/teams/team";
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
            return MapToDTO(loadedInvitation!);
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
            if (team.MentorId != null)
            {
                throw new Exception("This team already has a mentor.");
            }

            int activeTeamCount = await _invitationRepo.GetMentorActiveTeamCountAsync(mentorId, team.SemesterId);
            if (activeTeamCount >= 4)
            {
                throw new Exception("You have reached the maximum limit of 4 teams for this semester.");
            }

            team.MentorId = mentorId;
            await _teamRepo.UpdateAsync(team);
            await _invitationRepo.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Accepted);

            // Invalidate cache after accepting
            await _redisService.DeleteValueAsync(MentorInvitationsKey(mentorId));
            await _redisService.DeleteValueAsync(MentorActiveCountKey(mentorId));
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

        private MentorInvitationDTO MapToDTO(Teaminvitation entity)
        {
            return new MentorInvitationDTO
            {
                InvitationId = entity.InvitationId,
                TeamId = entity.TeamId,
                TeamName = entity.Team?.TeamName ?? string.Empty,
                TeamCode = entity.Team?.TeamCode ?? string.Empty,
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
        }
    }
}
