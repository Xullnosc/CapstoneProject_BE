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

        public UserService(
            IUserRepository userRepository, 
            ISemesterRepository semesterRepository, 
            ITeamMemberRepository teamMemberRepository,
            ITeamInvitationRepository teamInvitationRepository)
        {
            _userRepository = userRepository;
            _semesterRepository = semesterRepository;
            _teamMemberRepository = teamMemberRepository;
            _teamInvitationRepository = teamInvitationRepository;
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
    }
}
