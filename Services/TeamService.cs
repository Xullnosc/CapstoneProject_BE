using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using Services.Helpers;
using BusinessObjects.Interfaces;

namespace Services
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryHelper _cloudinaryHelper;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IThesisRepository _thesisRepository;
        private readonly ISemesterService _semesterService;
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ICampusContextService _campusContextService;

        public TeamService(
            ITeamRepository teamRepository, 
            ISemesterRepository semesterRepository, 
            IUserRepository userRepository, 
            ICloudinaryHelper cloudinaryHelper, 
            ITeamMemberRepository teamMemberRepository,
            IThesisRepository thesisRepository,
            ISemesterService semesterService,
            IWhitelistRepository whitelistRepository,
            ICampusContextService campusContextService)
        {
            _teamRepository = teamRepository;
            _semesterRepository = semesterRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
            _teamMemberRepository = teamMemberRepository;
            _thesisRepository = thesisRepository;
            _semesterService = semesterService;
            _whitelistRepository = whitelistRepository;
            _campusContextService = campusContextService;
        }

        public async Task<TeamDTO> CreateTeamAsync(int leaderId, CreateTeamDTO createTeamDto)
        {
            // 1. Validate Semester
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester == null)
            {
                 throw new InvalidOperationException("Không tìm thấy kỳ học hiện tại.");
            }

            // [LIFECYCLE GUARD] Chỉ cho phép tạo nhóm trong giai đoạn Active
            if (currentSemester.Status != CampusConstants.SemesterStatus.Active)
            {
                throw new InvalidOperationException($"Kỳ học đang ở trạng thái '{currentSemester.Status}'. Không thể tạo nhóm mới.");
            }

            // 2. Validate User is whitelisted for current semester
            var leader = await _userRepository.GetByIdAsync(leaderId);
            if (leader == null) throw new KeyNotFoundException("User not found.");

            var isWhitelisted = await _whitelistRepository.IsWhitelistedInSemesterAsync(leader.Email, currentSemester.SemesterId);
            if (!isWhitelisted)
            {
                throw new UnauthorizedAccessException("Bạn không có tên trong danh sách tham gia học kỳ hiện tại.");
            }

            // 3. Validate User not in another team
            var existingTeam = await _teamRepository.GetTeamByStudentIdAsync(leaderId, currentSemester.SemesterId);
            if (existingTeam != null)
            {
                throw new InvalidOperationException("You are already a member of another team in this semester.");
            }

            // 4. Generate Team Code
            string teamCode = await GenerateTeamCodeAsync(currentSemester.SemesterId, currentSemester.SemesterCode);

            var campusId = _campusContextService.GetCurrentCampusId() 
                ?? throw new InvalidOperationException("Yêu cầu Campus Context hợp lệ để tạo nhóm.");

            // 4. Create Team Entity
            var team = new Team
            {
                CampusId = campusId,
                TeamCode = teamCode,
                TeamName = createTeamDto.TeamName,
                Description = !string.IsNullOrEmpty(createTeamDto.Description) ? createTeamDto.Description : "A proactively created team for Capstone Project.",
                TeamAvatar = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(createTeamDto.TeamName) + "&background=random&color=fff",
                SemesterId = currentSemester.SemesterId,
                LeaderId = leaderId,
                Status = "Insufficient",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 6. Add Leader as Member

            var member = new Teammember
            {
                StudentId = leaderId,
                Role = "Leader",
                JoinedAt = DateTime.UtcNow,
                Student = leader
            };
            team.Teammembers.Add(member);

            var createdTeam = await _teamRepository.CreateAsync(team);
            await _semesterService.InvalidateSemesterCacheAsync(currentSemester.SemesterId);
            return MapToDTO(createdTeam);
        }

        private async Task<string> GenerateTeamCodeAsync(int semesterId, string semesterCode)
        {
            // Prefix: [SemesterCode]_SE_ (e.g., SP26_SE_)
            string validPrefix = $"{semesterCode}_SE_";
            var teamCodes = await _teamRepository.GetTeamCodesBySemesterAsync(semesterId);
            
            if (teamCodes == null || !teamCodes.Any())
            {
                return $"{validPrefix}01";
            }

            // Optimized LINQ:
            // 1. Filter codes starting with the semester-specific prefix
            // 2. Extract number part
            // 3. Parse to int
            // 4. Find Max
            int maxId = teamCodes
                .Where(code => code.StartsWith(validPrefix))
                .Select(code => code.Substring(validPrefix.Length))
                .Select(numPart => int.TryParse(numPart, out int id) ? id : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{validPrefix}{maxId + 1:D2}";
        }

        public async Task<TeamDTO?> GetTeamByIdAsync(int teamId, int userId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) return null;

            // 1. Check if user is a member
            bool isMember = team.Teammembers.Any(tm => tm.StudentId == userId);
            
            // 2. Check if user is a Mentor
            bool isMentor = team.MentorId == userId || team.MentorId2 == userId;
            
            // 3. Check if user is HOD
            var user = await _userRepository.GetByIdAsync(userId);
            bool isHod = user?.Role != null && string.Equals(user.Role.RoleName, CampusConstants.Roles.HOD, StringComparison.OrdinalIgnoreCase);

            if (!isMember && !isMentor && !isHod)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this team details.");
            }

            return MapToDTO(team);
        }

        public async Task<List<TeamDTO>> GetTeamsBySemesterAsync(int semesterId)
        {
            var teams = await _teamRepository.GetBySemesterAsync(semesterId);
            return teams.Select(MapToDTO).ToList();
        }

        public async Task<PagedResult<TeamDTO>> GetTeamsBySemesterPagedAsync(int semesterId, int page, int limit)
        {
            var (items, totalCount) = await _teamRepository.GetBySemesterPagedAsync(semesterId, page, limit);
            var dtos = items.Select(MapToDTO).ToList();
            return new PagedResult<TeamDTO>(dtos, totalCount, page, limit);
        }

        public async Task<bool> DisbandTeamAsync(int teamId, int leaderId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) return false;

            // [LIFECYCLE GUARD]
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester?.Status == CampusConstants.SemesterStatus.ReviewMiddle || currentSemester?.Status == CampusConstants.SemesterStatus.Closed)
            {
                throw new InvalidOperationException("Kỳ học đã chốt hoặc kết thúc. Không thể giải tán nhóm.");
            }

            if (team.LeaderId != leaderId)
            {
                throw new Exception("Only the team leader can disband the team.");
            }

            // TODO: Check if team has matched topic (when Topic module implemented)
            // if (team.TopicId != null && !semesterEnded) throw ...

            team.Status = CampusConstants.TeamStatus.Disbanded;
            team.UpdatedAt = DateTime.UtcNow;
            await _teamRepository.UpdateAsync(team);
            return true;
        }

        public async Task<TeamDTO?> GetTeamByStudentIdAsync(int studentId)
        {
             var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
             if (currentSemester == null) return null;

             var team = await _teamRepository.GetTeamByStudentIdAsync(studentId, currentSemester.SemesterId);
             if (team == null)
             {
                 // Check if user is a mentor for any team in this semester
                 var mentoredTeams = await _teamRepository.GetBySemesterAsync(currentSemester.SemesterId);
                 team = mentoredTeams.FirstOrDefault(t => t.MentorId == studentId || t.MentorId2 == studentId);
             }
             return team == null ? null : MapToDTO(team);
        }

        public async Task<List<TeamDTO>> GetMentorTeamsAsync(int mentorId)
        {
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester == null) return new List<TeamDTO>();

            var allTeams = await _teamRepository.GetBySemesterAsync(currentSemester.SemesterId);
            return allTeams
                .Where(t => (t.MentorId == mentorId || t.MentorId2 == mentorId) && t.Status != "Disbanded")
                .Select(MapToDTO)
                .ToList();
        }

        private TeamDTO MapToDTO(Team team)
        {
            return new TeamDTO
            {
                TeamId = team.TeamId,
                TeamCode = DisplayHelper.FormatTeamCode(team.TeamCode),
                TeamName = team.TeamName,
                TeamAvatar = team.TeamAvatar,
                Description = team.Description,
                SemesterId = team.SemesterId,
                LeaderId = team.LeaderId,
                Status = team.Status,
                MemberCount = team.Teammembers?.Count ?? 0,
                IsSpecial = team.IsSpecial,
                CreatedAt = team.CreatedAt ?? DateTime.UtcNow,
                Members = team.Teammembers?.Select(tm => new TeamMemberDTO
                {
                    TeamMemberId = tm.TeamMemberId,
                    StudentId = tm.StudentId,
                    StudentCode = tm.Student?.StudentCode ?? "N/A", 
                    FullName = tm.Student?.FullName ?? "Unknown",
                    Email = tm.Student?.Email ?? "N/A",
                    Avatar = tm.Student?.Avatar,
                    Role = tm.Role,
                    JoinedAt = tm.JoinedAt ?? DateTime.UtcNow
                }).ToList() ?? new List<TeamMemberDTO>(),
                MentorId = team.MentorId,
                MentorName = team.Mentor?.FullName ?? (team.MentorId != null ? "Assigned Mentor" : string.Empty),
                MentorEmail = team.Mentor?.Email ?? string.Empty,
                MentorAvatar = team.Mentor?.Avatar ?? string.Empty,
                MentorId2 = team.MentorId2,
                Mentor2Name = team.Mentor2?.FullName ?? (team.MentorId2 != null ? "Assigned Mentor" : string.Empty),
                Mentor2Email = team.Mentor2?.Email ?? string.Empty,
                Mentor2Avatar = team.Mentor2?.Avatar ?? string.Empty
            };
        }

        public async Task<TeamDTO> UpdateTeamAsync(int teamId, int leaderId, UpdateTeamDTO updateTeamDto)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) throw new KeyNotFoundException("Team not found");

            // [LIFECYCLE GUARD]
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester?.Status == CampusConstants.SemesterStatus.ReviewMiddle || currentSemester?.Status == CampusConstants.SemesterStatus.Closed)
            {
                throw new InvalidOperationException("Kỳ học đã chốt hoặc kết thúc. Không thể cập nhật thông tin nhóm.");
            }

            if (team.LeaderId != leaderId)
            {
                throw new UnauthorizedAccessException("Only the team leader can update team information.");
            }

            team.TeamName = updateTeamDto.TeamName;
            team.Description = updateTeamDto.Description;
            team.UpdatedAt = DateTime.UtcNow;

            if (updateTeamDto.AvatarFile != null)
            {
                string avatarUrl = await _cloudinaryHelper.UploadImageAsync(updateTeamDto.AvatarFile);
                team.TeamAvatar = avatarUrl;
            }

            await _teamRepository.UpdateAsync(team);
            return MapToDTO(team);
        }

        public async Task<bool> ChangeLeaderAsync(int teamId, int currentLeaderId, int newLeaderId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) return false;

            // [LIFECYCLE GUARD]
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester?.Status == CampusConstants.SemesterStatus.ReviewMiddle || currentSemester?.Status == CampusConstants.SemesterStatus.Closed)
            {
                throw new InvalidOperationException("Kỳ học đã chốt hoặc kết thúc. Không thể thay đổi nhóm trưởng.");
            }

            if (team.LeaderId != currentLeaderId)
            {
                throw new UnauthorizedAccessException("Only the current team leader can transfer leadership.");
            }

            var newLeaderMember = team.Teammembers.FirstOrDefault(m => m.StudentId == newLeaderId);
            if (newLeaderMember == null)
            {
                throw new ArgumentException("The new leader must be a member of the team.");
            }

            // Update Roles
            var currentLeaderMember = team.Teammembers.FirstOrDefault(m => m.StudentId == currentLeaderId);
            if (currentLeaderMember != null)
            {
                currentLeaderMember.Role = "Member";
            }

            newLeaderMember.Role = "Leader";
            team.LeaderId = newLeaderId;
            team.UpdatedAt = DateTime.UtcNow;

            await _teamRepository.UpdateAsync(team);

            // Transfer Thesis Ownership
            var activeTheses = await _thesisRepository.GetThesesByUserIdAsync(currentLeaderId);
            var activeThesis = activeTheses.FirstOrDefault();
            if (activeThesis != null)
            {
                activeThesis.UserId = newLeaderId;
                await _thesisRepository.UpdateThesisAsync(activeThesis);
            }

            return true;
        }

        public async Task<bool> RemoveMemberAsync(int teamId, int studentId)
        {
            // [LIFECYCLE GUARD]
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester?.Status == CampusConstants.SemesterStatus.ReviewMiddle || currentSemester?.Status == CampusConstants.SemesterStatus.Closed)
            {
                throw new InvalidOperationException("Kỳ học đã chốt hoặc kết thúc. Không thể xóa thành viên khỏi nhóm.");
            }

            var result = await _teamMemberRepository.RemoveMemberAsync(teamId, studentId);
            if (result)
            {
                var team = await _teamRepository.GetByIdAsync(teamId);
                if (team != null)
                {
                    int count = team.Teammembers?.Count ?? 1; // It already fetched without the removed member
                    string newStatus = count switch
                    {
                        >= 4 => CampusConstants.TeamStatus.Active,
                        3 => CampusConstants.TeamStatus.PendingApproval,
                        _ => CampusConstants.TeamStatus.Insufficient
                    };

                    if (newStatus != team.Status)
                    {
                        await _teamRepository.UpdateStatusAsync(teamId, newStatus);
                    }
                }
                await _semesterService.InvalidateSemesterCacheAsync();
            }
            return result;
        }

        public async Task<bool> ToggleSpecialFlagAsync(int teamId, int hodUserId)
        {
            var user = await _userRepository.GetByIdAsync(hodUserId);
            if (user == null || user.Role?.RoleName != CampusConstants.Roles.HOD)
            {
                throw new UnauthorizedAccessException("Only Head of Department can toggle the special flag.");
            }

            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) return false;

            team.IsSpecial = !team.IsSpecial;
            team.UpdatedAt = DateTime.UtcNow;

            await _teamRepository.UpdateAsync(team);
            await _semesterService.InvalidateSemesterCacheAsync();
            return true;
        }
        public async Task<TeamDTO> ForceCreateTeamAsync(int hodUserId, ForceCreateTeamDTO dto)
        {
            // 1. Validate HOD
            var hodUser = await _userRepository.GetByIdAsync(hodUserId);
            if (hodUser == null || hodUser.Role?.RoleName != CampusConstants.Roles.HOD)
                throw new UnauthorizedAccessException("Only Head of Department can force-create teams.");

            // 2. Validate Semester
            var semester = await _semesterRepository.GetSemesterByIdAsync(dto.SemesterId);
            if (semester == null)
                throw new KeyNotFoundException($"Semester {dto.SemesterId} not found.");

            // 3. Validate LeaderEmail is in MemberEmails
            if (!dto.MemberEmails.Contains(dto.LeaderEmail))
                throw new ArgumentException("Leader must be included in the members list.");

            // 4. Resolve emails to users and validate
            var uniqueEmails = dto.MemberEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var users = await _userRepository.GetUsersByEmailsAsync(uniqueEmails);
            
            var semesterWhitelists = await _whitelistRepository.GetBySemesterIdAsync(dto.SemesterId);
            var whitelistedStudentEmails = new HashSet<string>(
                semesterWhitelists
                    .Where(w => !string.IsNullOrEmpty(w.Email) && w.Role?.RoleName == CampusConstants.Roles.Student)
                    .Select(w => w.Email.ToLower().Trim()),
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var email in uniqueEmails)
            {
                var user = users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                    throw new ArgumentException($"User with email '{email}' not found.");
                
                // Use Whitelist as source of truth for Role in this semester
                if (!whitelistedStudentEmails.Contains(email))
                {
                    throw new ArgumentException($"User {user.FullName ?? user.Email} is not authorized as a student for the semester '{semester.SemesterCode}'. Please check the Whitelist.");
                }

                var existingTeam = await _teamRepository.GetTeamByStudentIdAsync(user.UserId, dto.SemesterId);
                if (existingTeam != null)
                    throw new InvalidOperationException($"Student {user.FullName ?? user.Email} is already in team '{existingTeam.TeamName}'.");
            }

            // 5. Find leader user
            var leader = users.First(u => string.Equals(u.Email, dto.LeaderEmail, StringComparison.OrdinalIgnoreCase));

            // 6. Generate TeamCode
            string teamCode = await GenerateTeamCodeAsync(dto.SemesterId, semester.SemesterCode);

            // 7. Validate member count and set status
            if (uniqueEmails.Count > 5)
                throw new ArgumentException("A team can have at most 5 members.");

            bool isSpecial = uniqueEmails.Count < 4;
            // Force-created teams are typically Qualified if they are special or meet min count
            string status = CampusConstants.TeamStatus.Active; 

            var campusId = _campusContextService.GetCurrentCampusId() 
                ?? throw new InvalidOperationException("Yêu cầu Campus Context hợp lệ. HOD hãy chọn Campus cụ thể.");

            // 8. Create Team
            var team = new Team
            {
                CampusId = campusId,
                TeamCode = teamCode,
                TeamName = dto.TeamName,
                Description = dto.Description ?? "Team created by HOD.",
                TeamAvatar = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(dto.TeamName) + "&background=random&color=fff",
                SemesterId = dto.SemesterId,
                LeaderId = leader.UserId,
                Status = status,
                IsSpecial = isSpecial,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 9. Add all members
            foreach (var user in users)
            {
                team.Teammembers.Add(new Teammember
                {
                    StudentId = user.UserId,
                    Role = user.UserId == leader.UserId ? CampusConstants.TeamRole.Leader : CampusConstants.TeamRole.Member,
                    JoinedAt = DateTime.UtcNow
                });
            }

            var createdTeam = await _teamRepository.CreateAsync(team);
            await _semesterService.InvalidateSemesterCacheAsync(dto.SemesterId);
            return MapToDTO(createdTeam);
        }
    }
}
