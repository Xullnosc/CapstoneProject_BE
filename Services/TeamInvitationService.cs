using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class TeamInvitationService : ITeamInvitationService
    {
        private readonly ITeamInvitationRepository _invitationRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUserRepository _userRepository;

        public TeamInvitationService(
            ITeamInvitationRepository invitationRepository,
            ITeamMemberRepository teamMemberRepository,
            ITeamRepository teamRepository,
            ISemesterRepository semesterRepository,
            IUserRepository userRepository)
        {
            _invitationRepository = invitationRepository;
            _teamMemberRepository = teamMemberRepository;
            _teamRepository = teamRepository;
            _semesterRepository = semesterRepository;
            _userRepository = userRepository;
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
             throw new NotImplementedException("Send Invitation not implemented yet");
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
                    Name = inv.InvitedByNavigation?.FullName ?? "Unknown"
                },
                Status = inv.Status,
                CreatedAt = inv.CreatedAt ?? DateTime.UtcNow
            };
        }

        #endregion
    }
}
