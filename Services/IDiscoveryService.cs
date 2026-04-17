using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;

namespace Services
{
    public interface IDiscoveryService
    {
        Task<PagedResult<DiscoveryStudentDto>> GetLookingStudentsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize);

        Task<PagedResult<DiscoveryTeamDto>> GetOpenTeamsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize);


        Task UpdateUserSkillsAsync(int userId, List<SkillEntry> skills);

        Task<List<string>> GetPopularSkillsAsync();

        Task<List<UserSkillDto>> GetUserSkillsAsync(int userId);
        Task RequestToJoinAsync(int studentId, int teamId);
        Task CancelJoinRequestAsync(int studentId, int teamId);
    }
}
