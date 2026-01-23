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
        Task<bool> UpdateStatusAsync(int teamId, string status);
        Task<int> CountTeamsInSemesterAsync(int semesterId);
        Task<Team?> GetTeamByStudentIdAsync(int studentId, int semesterId);
    }
}
