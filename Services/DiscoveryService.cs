using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;

namespace Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private readonly IDiscoveryRepository _discoveryRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly INotificationService _notificationService;

        public DiscoveryService(IDiscoveryRepository discoveryRepository, IUserRepository userRepository, ITeamRepository teamRepository, ISemesterRepository semesterRepository, INotificationService notificationService)
        {
            _discoveryRepository = discoveryRepository;
            _userRepository = userRepository;
            _teamRepository = teamRepository;
            _semesterRepository = semesterRepository;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<DiscoveryStudentDto>> GetLookingStudentsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize)
        {
            var (items, totalCount) = await _discoveryRepository.GetLookingStudentsAsync(
                semesterId, campusId, currentUserId, skillFilter, searchQuery, page, pageSize);

            return new PagedResult<DiscoveryStudentDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<PagedResult<DiscoveryTeamDto>> GetOpenTeamsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize)
        {
            var (items, totalCount) = await _discoveryRepository.GetOpenTeamsAsync(
                semesterId, campusId, currentUserId, skillFilter, searchQuery, page, pageSize);

            return new PagedResult<DiscoveryTeamDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task UpdateUserSkillsAsync(int userId, List<SkillEntry> skills)
        {
            var userSkills = skills.Select(s => new UserSkill
            {
                UserId = userId,
                SkillTag = s.SkillTag,
                SkillLevel = s.SkillLevel
            }).ToList();

            await _discoveryRepository.UpdateUserSkillsAsync(userId, userSkills);
        }

        public async Task<List<string>> GetPopularSkillsAsync()
        {
            return await _discoveryRepository.GetTopSkillsAsync(10);
        }

        public async Task<List<UserSkillDto>> GetUserSkillsAsync(int userId)
        {
            var skills = await _discoveryRepository.GetUserSkillsAsync(userId);
            return skills.Select(s => new UserSkillDto(s.SkillId, s.SkillTag, s.SkillLevel)).ToList();
        }

        public async Task RequestToJoinAsync(int studentId, int teamId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) throw new InvalidOperationException("Team not found.");
            
            var student = await _userRepository.GetByIdAsync(studentId);
            if (student == null) throw new InvalidOperationException("Student not found.");

            // 1. [LIFECYCLE GUARD] Chỉ cho phép gửi yêu cầu khi kỳ học ở trạng thái Open
            var currentSemesterCheck = await _semesterRepository.GetCurrentSemesterAsync();
            if (!CampusConstants.SemesterStatus.IsOpenStage(currentSemesterCheck?.Status))
            {
                throw new InvalidOperationException($"Kỳ học đang ở trạng thái '{currentSemesterCheck?.Status}'. Chỉ có thể gửi yêu cầu gia nhập khi kỳ học đang mở (Open).");
            }

            // 2. Safety check: Cannot join if already in a team
            bool alreadyInTeam = await _discoveryRepository.IsUserInTeamAsync(studentId, team.SemesterId);
            if (alreadyInTeam)
            {
                throw new InvalidOperationException("You are already a member of a team in this semester.");
            }

            // Safety check: Team must not be full
            if (team.Teammembers != null && team.Teammembers.Count >= 5)
            {
                throw new InvalidOperationException("This team has already reached the maximum capacity (5 members).");
            }

            // Safety check: Campus match
            if (team.CampusId != student.CampusId)
            {
                throw new InvalidOperationException("You cannot join a team from a different campus.");
            }

            // Duplicate spam prevention is handled cleanly in TeamDAO
            bool wasCreated = await _teamRepository.AddJoinRequestAsync(studentId, teamId);

            if (wasCreated)
            {
                await _notificationService.CreateNotificationAsync(
                    team.LeaderId,
                    NotificationType.TeamInvitation.ToString(),
                    "New Join Request",
                    $"{student.FullName} has requested to join your team {team.TeamName}.",
                    "Team",
                    team.TeamId,
                    sendEmail: true
                );
            }
        }

        public async Task CancelJoinRequestAsync(int studentId, int teamId)
        {
            // [LIFECYCLE GUARD] Chỉ cho phép hủy yêu cầu khi kỳ học ở trạng thái Open
            var currentSemesterCheck = await _semesterRepository.GetCurrentSemesterAsync();
            if (!CampusConstants.SemesterStatus.IsOpenStage(currentSemesterCheck?.Status))
            {
                throw new InvalidOperationException($"Kỳ học đang ở trạng thái '{currentSemesterCheck?.Status}'. Chỉ có thể hủy yêu cầu khi kỳ học đang mở (Open).");
            }
            
            await _teamRepository.CancelJoinRequestAsync(studentId, teamId);
        }
    }
}
