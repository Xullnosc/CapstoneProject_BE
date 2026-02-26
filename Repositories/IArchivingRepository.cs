using BusinessObjects.Models;

namespace Repositories
{
    public interface IArchivingRepository
    {
        Task ArchiveTeamAsync(ArchivedTeam team);

        Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterAsync(int semesterId);
        Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterIdsAsync(List<int> semesterIds);
        Task<List<ArchivedTeam>> GetAllArchivedTeamsAsync();
        Task<List<ArchivedWhitelist>> GetArchivedWhitelistsBySemesterIdsAsync(List<int> semesterIds);
        Task ArchiveWhitelistsAsync(IEnumerable<ArchivedWhitelist> archivedWhitelists);
        Task ArchiveTeamsAsync(IEnumerable<ArchivedTeam> archivedTeams);
    }
}
