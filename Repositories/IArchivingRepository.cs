using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Repositories
{
    public interface IArchivingRepository
    {
        Task ArchiveTeamAsync(ArchivedTeam team);

        Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterAsync(int semesterId);
        Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterIdsAsync(List<int> semesterIds);
        Task<PagedResult<ArchivedTeam>> GetArchivedTeamsBySemesterAsync(int semesterId, int pageIndex, int limit);
        Task<PagedResult<ArchivedTeam>> GetArchivedTeamsBySemesterIdsAsync(List<int> semesterIds, int pageIndex, int limit);
        Task<List<ArchivedTeam>> GetAllArchivedTeamsAsync();
        Task<PagedResult<ArchivedTeam>> GetAllArchivedTeamsAsync(int pageIndex, int limit);
        Task<List<ArchivedWhitelist>> GetArchivedWhitelistsBySemesterIdsAsync(List<int> semesterIds);
        Task<PagedResult<ArchivedWhitelist>> GetArchivedWhitelistsBySemesterIdsAsync(List<int> semesterIds, int pageIndex, int limit);
        Task ArchiveWhitelistsAsync(IEnumerable<ArchivedWhitelist> archivedWhitelists);
        Task ArchiveTeamsAsync(IEnumerable<ArchivedTeam> archivedTeams);
    }
}
