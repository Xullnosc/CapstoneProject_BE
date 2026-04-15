using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Repositories
{
    public interface IDiscoveryRepository
    {
        Task<(List<DiscoveryStudentDto> Items, int TotalCount)> GetLookingStudentsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize);

        Task<(List<DiscoveryTeamDto> Items, int TotalCount)> GetOpenTeamsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize);

        Task<bool> IsUserInTeamAsync(int userId, int semesterId);

        Task<List<UserSkill>> GetUserSkillsAsync(int userId);
        Task<List<string>> GetTopSkillsAsync(int count);
        Task UpdateUserSkillsAsync(int userId, List<UserSkill> skills);
    }
}
