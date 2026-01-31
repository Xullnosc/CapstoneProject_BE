using BusinessObjects.Models;
using System.Threading.Tasks;

namespace Services
{
    public interface IArchivingService
    {
        Task ArchiveSemesterAsync(int semesterId);
        Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterAsync(int semesterId);
        Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterIdsAsync(List<int> semesterIds);
        Task<List<ArchivedTeam>> GetAllArchivedTeamsAsync();
        Task<List<ArchivedWhitelist>> GetArchivedWhitelistsBySemesterIdsAsync(List<int> semesterIds);
    }
}
