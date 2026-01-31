using BusinessObjects.Models;
using Repositories;
using System.Threading.Tasks;

namespace Services
{
    public class ArchivingService : IArchivingService
    {
        private readonly IArchivingRepository _archivingRepository;

        public ArchivingService(IArchivingRepository archivingRepository)
        {
            _archivingRepository = archivingRepository;
        }

        public async Task ArchiveSemesterAsync(int semesterId)
        {
            await _archivingRepository.ArchiveSemesterAsync(semesterId);
        }
        public async Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterAsync(int semesterId)
        {
            return await _archivingRepository.GetArchivedTeamsBySemesterAsync(semesterId);
        }

        public async Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterIdsAsync(List<int> semesterIds)
        {
            return await _archivingRepository.GetArchivedTeamsBySemesterIdsAsync(semesterIds);
        }

        public async Task<List<ArchivedTeam>> GetAllArchivedTeamsAsync()
        {
            return await _archivingRepository.GetAllArchivedTeamsAsync();
        }

        public async Task<List<ArchivedWhitelist>> GetArchivedWhitelistsBySemesterIdsAsync(List<int> semesterIds)
        {
            return await _archivingRepository.GetArchivedWhitelistsBySemesterIdsAsync(semesterIds);
        }
    }
}
