using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace DataAccess
{
    public interface ITeamDAO
    {
        Task<Team> CreateAsync(Team team);
        Task<Team?> GetByIdAsync(int teamId);
        Task<Team?> GetByCodeAsync(string teamCode);
        Task<List<Team>> GetBySemesterAsync(int semesterId);
        Task<PagedResult<Team>> GetBySemesterAsync(int semesterId, int pageIndex, int pageSize);
        Task<(List<Team> Items, int TotalCount)> GetBySemesterPagedAsync(int semesterId, int page, int limit);
        Task<bool> UpdateStatusAsync(int teamId, string status);
        Task<int> CountTeamsInSemesterAsync(int semesterId);
        Task<List<string>> GetTeamCodesBySemesterAsync(int semesterId);
        Task<PagedResult<string>> GetTeamCodesBySemesterAsync(int semesterId, int pageIndex, int pageSize);
        Task<Team?> GetTeamByStudentIdAsync(int studentId, int semesterId);
        Task<bool> UpdateAsync(Team team);
        Task<List<Team>> GetForArchivingAsync(int semesterId);
        Task<PagedResult<Team>> GetForArchivingAsync(int semesterId, int pageIndex, int pageSize);
        Task DeleteRangeAsync(IEnumerable<Team> teams);
        Task DeleteAsync(Team team);
        Task<Team?> GetActiveTeamByStudentIdAsync(int studentId);
        Task<List<Team>> GetTeamsByMentorIdAsync(int mentorId, int semesterId);
        Task<bool> AddJoinRequestAsync(int studentId, int teamId);
        Task CancelJoinRequestAsync(int studentId, int teamId);
    }
}
