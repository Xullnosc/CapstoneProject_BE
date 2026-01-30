using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;

namespace Repositories
{
    public interface ITeamRepository
    {
        Task<Team> CreateAsync(Team team);
        Task<Team?> GetByIdAsync(int teamId);
        Task<Team?> GetByCodeAsync(string teamCode);
        Task<List<Team>> GetBySemesterAsync(int semesterId);
        Task<(List<Team> Items, int TotalCount)> GetBySemesterPagedAsync(int semesterId, int page, int limit);
        Task<bool> UpdateStatusAsync(int teamId, string status);
        Task<int> CountTeamsInSemesterAsync(int semesterId);
        Task<List<string>> GetTeamCodesBySemesterAsync(int semesterId);
        Task<Team?> GetTeamByStudentIdAsync(int studentId, int semesterId);
        Task<bool> UpdateAsync(Team team);
    }
}
