using BusinessObjects.Models;
using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IArchivedTeamDAO
    {
        Task AddRangeAsync(IEnumerable<ArchivedTeam> archivedTeams);
        Task AddAsync(ArchivedTeam archivedTeam);
        Task<List<ArchivedTeam>> GetBySemesterIdAsync(int semesterId);
        Task<List<ArchivedTeam>> GetBySemesterIdsAsync(List<int> semesterIds);
        Task<PagedResult<ArchivedTeam>> GetArchivedTeamsBySemesterAsync(int semesterId, int pageIndex, int limit);
        Task<PagedResult<ArchivedTeam>> GetArchivedTeamsBySemesterIdsAsync(List<int> semesterIds, int pageIndex, int limit);
        Task<List<ArchivedTeam>> GetAllAsync();
        Task<PagedResult<ArchivedTeam>> GetAllAsync(int pageIndex, int limit);
    }
}
