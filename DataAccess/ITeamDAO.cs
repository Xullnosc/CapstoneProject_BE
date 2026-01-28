using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;

namespace DataAccess
{
    public interface ITeamDAO
    {
        Task<Team> CreateAsync(Team team);
        Task<Team?> GetByIdAsync(int teamId);
        Task<Team?> GetByCodeAsync(string teamCode);
        Task<List<Team>> GetBySemesterAsync(int semesterId);
        Task<bool> UpdateStatusAsync(int teamId, string status);
        Task<int> CountTeamsInSemesterAsync(int semesterId);
        Task<List<string>> GetTeamCodesBySemesterAsync(int semesterId);
        Task<Team?> GetTeamByStudentIdAsync(int studentId, int semesterId);
        Task<bool> UpdateAsync(Team team);
        Task<List<Team>> GetForArchivingAsync(int semesterId);
        Task DeleteRangeAsync(IEnumerable<Team> teams);
        Task DeleteAsync(Team team);
    }
}
