using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class TeamInvitationService : ITeamInvitationService
    {
        private readonly ITeamInvitationRepository _invitationRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public TeamInvitationService(
            ITeamInvitationRepository invitationRepository,
            ITeamMemberRepository teamMemberRepository,
            ITeamRepository teamRepository,
            ISemesterRepository semesterRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _invitationRepository = invitationRepository;
            _teamMemberRepository = teamMemberRepository;
            _teamRepository = teamRepository;
            _semesterRepository = semesterRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<List<TeamInvitationDTO>> GetMyInvitationsAsync(int studentId)
        {
            var invitations = await _invitationRepository.GetPendingInvitationsByStudentAsync(studentId);
            return invitations.Select(MapToDTO).ToList();
        }

        public async Task AcceptInvitationAsync(int invitationId, int studentId)
        {
            var invitation = await ValidateInvitationAsync(invitationId, studentId);

            // 1. Check eligibility (Student not in team, Semester active)
            await EnsureStudentCanJoinTeamAsync(studentId);

            // 2. Check and Retrieve Team
            var team = await ValidateTeamForJoinAsync(invitation.TeamId);

            // 3. Add Member
            await AddMemberToTeamAsync(team.TeamId, studentId);

            // 4. Update Invitation Status
            await _invitationRepository.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Accepted);

            // 5. Cancel other pending invitations
            await _invitationRepository.CancelAllPendingInvitationsForStudentAsync(studentId);

            // 6. Update Team Status if needed
            await UpdateTeamStatusAfterJoinAsync(team);
        }

        public async Task DeclineInvitationAsync(int invitationId, int studentId)
        {
            await ValidateInvitationAsync(invitationId, studentId);
            await _invitationRepository.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Declined);
        }

        public async Task<TeamInvitationDTO> SendInvitationAsync(int teamId, int inviterId, string studentCodeOrEmail)
        {
            // 1. Validate Team
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) throw new KeyNotFoundException("Team not found");
            if (team.LeaderId != inviterId) throw new UnauthorizedAccessException("Only the leader can invite members");
            
            // Check if team is full logic is handled in Accept, but good to check here too if strict
            if (team.Teammembers.Count >= 5) throw new InvalidOperationException("Team is full");

            // 2. Find Student
            // We search by email or student code
            var users = await _userRepository.SearchUsersAsync(studentCodeOrEmail);
            // SearchUsersAsync uses 'Contains', we want exact match for invite
            var student = users.FirstOrDefault(u => u.Email.Equals(studentCodeOrEmail, StringComparison.OrdinalIgnoreCase) 
                                                 || u.StudentCode.Equals(studentCodeOrEmail, StringComparison.OrdinalIgnoreCase));

            if (student == null) throw new KeyNotFoundException($"Student with email/code '{studentCodeOrEmail}' not found");
            
            // 3. Validate Student Eligibility
            if (student.UserId == inviterId) throw new InvalidOperationException("You cannot invite yourself");
            
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester != null)
            {
                bool alreadyInTeam = await _teamMemberRepository.IsStudentInTeamAsync(student.UserId, currentSemester.SemesterId);
                if (alreadyInTeam) throw new InvalidOperationException("Student is already in a team");
            }

            // 4. Check for existing pending invitation
            var existingInvite = await _invitationRepository.GetByTeamAndStudentAsync(teamId, student.UserId);
            if (existingInvite != null) throw new InvalidOperationException("Student has already been invited to this team");

            // 5. Create Invitation
            var invitation = new Teaminvitation
            {
                TeamId = teamId,
                StudentId = student.UserId,
                InvitedBy = inviterId,
                Status = CampusConstants.InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var createdInv = await _invitationRepository.CreateAsync(invitation);

            // Send Email Notification
            try
            {
                var inviter = await _userRepository.GetByIdAsync(inviterId);
                var inviterName = inviter?.FullName ?? "A Team Leader";
                var teamName = team.TeamName;
                var studentName = student.FullName;

<<<<<<< HEAD
                string subjectTemplate = _configuration["EmailTemplates:TeamInvitation:Subject"];
                string htmlBodyTemplate = _configuration["EmailTemplates:TeamInvitation:HtmlBody"];

                string subject = subjectTemplate
                    .Replace("{TeamName}", teamName);
                
                string htmlContent = htmlBodyTemplate
                    .Replace("{StudentName}", studentName)
                    .Replace("{TeamName}", teamName)
                    .Replace("{InviterName}", inviterName);
=======
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

                string link = $"{frontendUrl}/teams/my-team";

                string subject = $"[FCTMS] You have been invited to join Team {teamName}";
                
                string htmlBodyTemplate = EmailTemplateConstants.TeamInvitationTemplate; // Using a constant/static string

                string htmlContent = htmlBodyTemplate
                    .Replace("{StudentName}", studentName)
                    .Replace("{TeamName}", teamName)
                    .Replace("{InviterName}", inviterName)
                    .Replace("{InvitationLink}", link)
                    .Replace("{CurrentYear}", DateTime.Now.Year.ToString());
>>>>>>> 78181965ba97f8f708e3ab280a6fa309d2d472d4

             
               await _emailService.SendEmailAsync(student.Email, subject, htmlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamInvitationService] Failed to send email: {ex.Message}");
              
            }
            
            // Reload to get navigation props for DTO
            var fullInv = await _invitationRepository.GetByIdAsync(createdInv.InvitationId);
            return MapToDTO(fullInv!);
        }

        public async Task CancelInvitationAsync(int invitationId, int userId)
        {
            var invitation = await _invitationRepository.GetByIdAsync(invitationId);
            if (invitation == null) throw new KeyNotFoundException("Invitation not found");

        
            if (invitation.InvitedBy != userId)
            {
                 throw new UnauthorizedAccessException("You are not authorized to cancel this invitation");
            }

            if (invitation.Status != CampusConstants.InvitationStatus.Pending)
            {
                throw new InvalidOperationException("Cannot cancel an invitation that is not pending");
            }

            
            await _invitationRepository.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Cancelled);
        }

        #region Helper Methods

        private async Task<Teaminvitation> ValidateInvitationAsync(int invitationId, int studentId)
        {
            var invitation = await _invitationRepository.GetByIdAsync(invitationId);
            if (invitation == null) throw new KeyNotFoundException("Invitation not found");

            if (invitation.StudentId != studentId)
            {
                throw new UnauthorizedAccessException("You are not the recipient of this invitation");
            }

            if (invitation.Status != CampusConstants.InvitationStatus.Pending)
            {
                throw new InvalidOperationException($"Invitation is already {invitation.Status}");
            }
            return invitation;
        }

        private async Task EnsureStudentCanJoinTeamAsync(int studentId)
        {
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester == null) throw new Exception("Active semester not found");

            bool alreadyInTeam = await _teamMemberRepository.IsStudentInTeamAsync(studentId, currentSemester.SemesterId);
            if (alreadyInTeam)
            {
                throw new InvalidOperationException("You are already in a team for this semester");
            }
        }

        private async Task<Team> ValidateTeamForJoinAsync(int teamId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) throw new KeyNotFoundException("Team no longer exists");
            if (team.Status == CampusConstants.TeamStatus.Disbanded) 
                throw new InvalidOperationException("Team has been disbanded");

            if (team.Teammembers.Count >= 5)
            {
                 throw new InvalidOperationException("Team is already full (max 5 members)");
            }
            return team;
        }

        private async Task AddMemberToTeamAsync(int teamId, int studentId)
        {
            var newMember = new Teammember
            {
                TeamId = teamId,
                StudentId = studentId,
                Role = CampusConstants.TeamRole.Member,
                JoinedAt = DateTime.UtcNow
            };
            await _teamMemberRepository.AddMemberAsync(newMember);
        }

        private async Task UpdateTeamStatusAfterJoinAsync(Team team)
        {
            // Re-fetch members count or use loaded count + 1
            int newCount = team.Teammembers.Count + 1; 
            if (newCount >= 3 && team.Status == CampusConstants.TeamStatus.Insufficient)
            {
                await _teamRepository.UpdateStatusAsync(team.TeamId, CampusConstants.TeamStatus.Pending);
            }
        }

        private TeamInvitationDTO MapToDTO(Teaminvitation inv)
        {
            return new TeamInvitationDTO
            {
                InvitationId = inv.InvitationId,
                TeamId = inv.TeamId,
                Team = new TeamInfoDTO
                {
                    TeamId = inv.Team?.TeamId ?? 0,
                    TeamName = inv.Team?.TeamName ?? "Unknown Team",
                    TeamAvatar = inv.Team?.TeamAvatar,
                    MemberCount = inv.Team?.Teammembers?.Count ?? 0,
                    LeaderName = inv.Team?.Leader?.FullName ?? "Unknown" 
                },
                StudentId = inv.StudentId,
                InvitedBy = new InvitedByDTO
                {
                    UserId = inv.InvitedBy,
                    Name = inv.InvitedByNavigation?.FullName ?? "Unknown",
                    Email = inv.InvitedByNavigation?.Email ?? "",
                    Avatar = inv.InvitedByNavigation?.Avatar ?? ""
                },
                Status = inv.Status,
                CreatedAt = inv.CreatedAt ?? DateTime.UtcNow
            };
        }

        #endregion
    }
}
