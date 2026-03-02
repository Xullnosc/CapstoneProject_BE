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

            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            var semesterId = currentSemester?.SemesterId ?? 0;

            if (semesterId == 0) return new List<UserInfoDTO>();

            var whitelists = await _whitelistRepository.SearchAsync(term, semesterId);
            var emails = whitelists.Select(w => w.Email).ToList();
            var existingUsers = await _userRepository.GetUsersByEmailsAsync(emails);

            var result = new List<UserInfoDTO>();

            foreach (var w in whitelists)
            {
                var existingUser = existingUsers.FirstOrDefault(u => u.Email.Equals(w.Email, System.StringComparison.OrdinalIgnoreCase));

                // Exclude current user
                if (existingUser != null && existingUser.UserId == currentUserId) continue;

                var dto = new UserInfoDTO
                {
                    UserId = existingUser?.UserId ?? -w.WhitelistId, // Use negative id for unique UI key if not logged in
                    Email = w.Email,
                    FullName = w.FullName ?? existingUser?.FullName,
                    StudentCode = w.StudentCode ?? existingUser?.StudentCode,
                    Avatar = w.Avatar ?? existingUser?.Avatar
                };

                if (existingUser != null && semesterId > 0)
                {
                    dto.HasTeam = await _teamMemberRepository.IsStudentInTeamAsync(existingUser.UserId, semesterId);
                }

                if (teamId.HasValue && !dto.HasTeam && existingUser != null)
                {
                    var existingInvitation = await _teamInvitationRepository.GetByTeamAndStudentAsync(teamId.Value, existingUser.UserId);
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
