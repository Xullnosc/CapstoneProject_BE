using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using Services.Helpers;

namespace Services
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryHelper _cloudinaryHelper;
        private readonly IArchivingRepository _archivingRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        public TeamService(ITeamRepository teamRepository, ISemesterRepository semesterRepository, IUserRepository userRepository, ICloudinaryHelper cloudinaryHelper, IArchivingRepository archivingRepository, ITeamMemberRepository teamMemberRepository)
        {
            _teamRepository = teamRepository;
            _semesterRepository = semesterRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
            _archivingRepository = archivingRepository;
            _teamMemberRepository = teamMemberRepository;
        }

        public async Task<TeamDTO> CreateTeamAsync(int leaderId, CreateTeamDTO createTeamDto)
        {
            // 1. Validate Semester
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester == null)
            {
                 throw new InvalidOperationException("No active semester found at the moment.");
            }

            // 2. Validate User not in another team
            var existingTeam = await _teamRepository.GetTeamByStudentIdAsync(leaderId, currentSemester.SemesterId);
            if (existingTeam != null)
            {
                throw new InvalidOperationException("You are already a member of another team in this semester.");
            }

            // 3. Generate Team Code
            string teamCode = await GenerateTeamCodeAsync(currentSemester.SemesterId, currentSemester.SemesterName);

            // 4. Create Team Entity
            var team = new Team
            {
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

            // 5. Add Leader as Member
            var leader = await _userRepository.GetByIdAsync(leaderId);
            if (leader == null) throw new KeyNotFoundException("User not found.");

            var member = new Teammember
            {
                StudentId = leaderId,
                Role = "Leader",
                JoinedAt = DateTime.UtcNow,
                Student = leader
            };
            team.Teammembers.Add(member);

            var createdTeam = await _teamRepository.CreateAsync(team);
            return MapToDTO(createdTeam);
        }

        private async Task<string> GenerateTeamCodeAsync(int semesterId, string semesterName)
        {
            const string ValidPrefix = "SE_";
            var teamCodes = await _teamRepository.GetTeamCodesBySemesterAsync(semesterId);
            
            if (teamCodes == null || !teamCodes.Any())
            {
                return $"{ValidPrefix}01";
            }

            // Optimized LINQ:
            // 1. Filter codes starting with "SE_"
            // 2. Extract number part
            // 3. Parse to int (use valid numbers only)
            // 4. Find Max
            int maxId = teamCodes
                .Where(code => code.StartsWith(ValidPrefix))
                .Select(code => code.Substring(ValidPrefix.Length))
                .Select(numPart => int.TryParse(numPart, out int id) ? id : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{ValidPrefix}{maxId + 1:D2}";
        }

        public async Task<TeamDTO?> GetTeamByIdAsync(int teamId, int userId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) return null;

            // Check if user is a member of the team
            bool isMember = team.Teammembers.Any(tm => tm.StudentId == userId);
            if (!isMember)
            {
                throw new UnauthorizedAccessException("You are not a member of this team.");
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

            if (team.LeaderId != leaderId)
            {
                throw new Exception("Only the team leader can disband the team.");
            }

            // TODO: Check if team has matched topic (when Topic module implemented)
            // if (team.TopicId != null && !semesterEnded) throw ...

            await _archivingRepository.ArchiveTeamAsync(team);
            return true;
        }

        public async Task<TeamDTO?> GetTeamByStudentIdAsync(int studentId)
        {
             var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
             if (currentSemester == null) return null;

             var team = await _teamRepository.GetTeamByStudentIdAsync(studentId, currentSemester.SemesterId);
             return team == null ? null : MapToDTO(team);
        }

        private TeamDTO MapToDTO(Team team)
        {
            return new TeamDTO
            {
                TeamId = team.TeamId,
                TeamCode = team.TeamCode,
                TeamName = team.TeamName,
                TeamAvatar = team.TeamAvatar,
                Description = team.Description,
                SemesterId = team.SemesterId,
                LeaderId = team.LeaderId,
                Status = team.Status,
                MemberCount = team.Teammembers?.Count ?? 0,
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
                }).ToList() ?? new List<TeamMemberDTO>()
            };
        }

        public async Task<TeamDTO> UpdateTeamAsync(int teamId, int leaderId, UpdateTeamDTO updateTeamDto)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) throw new KeyNotFoundException("Team not found");

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

        public async Task<bool> RemoveMemberAsync(int teamId, int studentId)
        {
            return await _teamMemberRepository.RemoveMemberAsync(teamId, studentId);
        }
    }
}
