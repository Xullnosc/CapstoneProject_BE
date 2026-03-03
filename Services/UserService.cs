using Services.DTOs;
using Repositories;
using BusinessObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly ITeamInvitationRepository _teamInvitationRepository;
        private readonly IWhitelistRepository _whitelistRepository;

        public UserService(
            IUserRepository userRepository, 
            ISemesterRepository semesterRepository, 
            ITeamMemberRepository teamMemberRepository,
            ITeamInvitationRepository teamInvitationRepository,
            IWhitelistRepository whitelistRepository)
        {
            _userRepository = userRepository;
            _semesterRepository = semesterRepository;
            _teamMemberRepository = teamMemberRepository;
            _teamInvitationRepository = teamInvitationRepository;
            _whitelistRepository = whitelistRepository;
        }

        public async Task<List<UserInfoDTO>> SearchStudentsAsync(string term, int currentUserId, int? teamId = null)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new List<UserInfoDTO>();
            }

            var users = await _userRepository.SearchUsersAsync(term);
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            var semesterId = currentSemester?.SemesterId ?? 0;

            var result = new List<UserInfoDTO>();

            foreach (var u in users)
            {
                // Exclude current user
                if (u.UserId == currentUserId) continue;

                // Ensure user has Student role
                if (u.Role?.RoleName != CampusConstants.Roles.Student) continue;

                var dto = new UserInfoDTO
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    FullName = u.FullName,
                    StudentCode = u.StudentCode,
                    Avatar = u.Avatar
                };

                if (semesterId > 0)
                {
                    dto.HasTeam = await _teamMemberRepository.IsStudentInTeamAsync(u.UserId, semesterId);
                }

                if (teamId.HasValue && !dto.HasTeam)
                {
                    var existingInvitation = await _teamInvitationRepository.GetByTeamAndStudentAsync(teamId.Value, u.UserId);
                    if (existingInvitation != null && existingInvitation.Status == CampusConstants.InvitationStatus.Pending)
                    {
                        dto.PendingInvitationId = existingInvitation.InvitationId;
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<List<UserInfoDTO>> SearchLecturersAsync(string term, int currentUserId, int? teamId = null)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new List<UserInfoDTO>();
            }

            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester == null) return new List<UserInfoDTO>();

            // Search from Whitelist instead of User table
            var whitelistedLecturers = await _whitelistRepository.GetBySemesterIdAsync(currentSemester.SemesterId);
            
            // Filter by search term and role
            var filtered = whitelistedLecturers.Where(w => 
                (w.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) || 
                 w.Email.Contains(term, StringComparison.OrdinalIgnoreCase)) &&
                (w.Role?.RoleName == CampusConstants.Roles.Lecturer || w.RoleId == 2) // Assuming 2 is Lecturer if Role object is not loaded
            ).ToList();

            var result = new List<UserInfoDTO>();

            foreach (var w in filtered)
            {
                // Get User object to get UserId (Mapping Email to User)
                var user = await _userRepository.GetByEmailAsync(w.Email);
                
                if (user != null && user.UserId == currentUserId) continue;

                var dto = new UserInfoDTO
                {
                    UserId = user?.UserId ?? 0, // 0 if whitelisted but hasn't logged in yet
                    Email = w.Email,
                    FullName = w.FullName ?? string.Empty,
                    StudentCode = w.StudentCode,
                    Avatar = user?.Avatar,
                    HasTeam = false
                };

                if (teamId.HasValue && user != null)
                {
                    var existingInvitation = await _teamInvitationRepository.GetByTeamAndMentorAsync(teamId.Value, user.UserId);
                    if (existingInvitation != null && existingInvitation.Status == CampusConstants.InvitationStatus.Pending)
                    {
                        dto.PendingInvitationId = existingInvitation.InvitationId;
                    }
                }

                result.Add(dto);
            }

            return result;
        }
    }
}
