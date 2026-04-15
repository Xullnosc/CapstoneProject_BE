using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;

namespace DataAccess
{
    public interface IDiscoveryDAO
    {
        Task<(List<User> Items, int TotalCount)> GetLookingStudentsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize);
        Task<(List<Team> Items, int TotalCount)> GetOpenTeamsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize);

        Task<List<string>> GetTopSkillsAsync(int count);
        Task UpdateUserSkillsAsync(int userId, List<UserSkill> skills);
        Task<bool> IsUserInTeamAsync(int userId, int semesterId);
        Task<List<UserSkill>> GetUserSkillsAsync(int userId);
    }
}
