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
        private readonly CloudinaryHelper _cloudinaryHelper;

        public TeamService(ITeamRepository teamRepository, ISemesterRepository semesterRepository, IUserRepository userRepository, CloudinaryHelper cloudinaryHelper)
        {
            _teamRepository = teamRepository;
            _semesterRepository = semesterRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
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
                Description = createTeamDto.Description,
                TeamAvatar = "https://cdn.haitrieu.com/wp-content/uploads/2021/10/Logo-Dai-hoc-FPT.png", // Default FPT Logo
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
            var teamCodes = await _teamRepository.GetTeamCodesBySemesterAsync(semesterId);
            if (teamCodes == null || !teamCodes.Any())
            {
                return $"{DateTime.Now.Year}-{semesterName}-001";
            }

            int maxId = 0;
            foreach (var code in teamCodes)
            {
                string[] parts = code.Split('-');
                if (parts.Length > 0 && int.TryParse(parts.Last(), out int id))
                {
                    if (id > maxId) maxId = id;
                }
            }

            return $"{DateTime.Now.Year}-{semesterName}-{maxId + 1:D3}";
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

            return await _teamRepository.UpdateStatusAsync(teamId, "Disbanded");
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
                MemberCount = team.Teammembers.Count,
                CreatedAt = team.CreatedAt ?? DateTime.UtcNow,
                Members = team.Teammembers.Select(tm => new TeamMemberDTO
                {
                    TeamMemberId = tm.TeamMemberId,
                    StudentId = tm.StudentId,
                    StudentCode = tm.Student.StudentCode, 
                    FullName = tm.Student.FullName,
                    Email = tm.Student.Email,
                    Avatar = tm.Student.Avatar,
                    Role = tm.Role,
                    JoinedAt = tm.JoinedAt ?? DateTime.UtcNow
                }).ToList()
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
    }
}
