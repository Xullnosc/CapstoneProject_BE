using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class ArchivingRepository : IArchivingRepository
    {
        private readonly FctmsContext _context;
        private readonly ITeamDAO _teamDAO;
        private readonly IWhitelistDAO _whitelistDAO;
        private readonly IArchivedTeamDAO _archivedTeamDAO;
        private readonly IArchivedWhitelistDAO _archivedWhitelistDAO;

        public ArchivingRepository(
            FctmsContext context,
            ITeamDAO teamDAO,
            IWhitelistDAO whitelistDAO,
            IArchivedTeamDAO archivedTeamDAO,
            IArchivedWhitelistDAO archivedWhitelistDAO)
        {
            _context = context;
            _teamDAO = teamDAO;
            _whitelistDAO = whitelistDAO;
            _archivedTeamDAO = archivedTeamDAO;
            _archivedWhitelistDAO = archivedWhitelistDAO;
        }

        public async Task ArchiveWhitelistsAsync(IEnumerable<ArchivedWhitelist> archivedWhitelists)
        {
            await _archivedWhitelistDAO.AddRangeAsync(archivedWhitelists);
        }
        public async Task ArchiveTeamsAsync(IEnumerable<ArchivedTeam> archivedTeams)
        {
            await _archivedTeamDAO.AddRangeAsync(archivedTeams);
        }

        public async Task ArchiveTeamAsync(ArchivedTeam archivedTeam)
        {
            await _archivedTeamDAO.AddAsync(archivedTeam);
        }
        public async Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterAsync(int semesterId)
        {
            return await _archivedTeamDAO.GetBySemesterIdAsync(semesterId);
        }

        public async Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterIdsAsync(List<int> semesterIds)
        {
            return await _archivedTeamDAO.GetBySemesterIdsAsync(semesterIds);
        }

        public async Task<List<ArchivedTeam>> GetAllArchivedTeamsAsync()
        {
            return await _archivedTeamDAO.GetAllAsync();
        }

        public async Task<List<ArchivedWhitelist>> GetArchivedWhitelistsBySemesterIdsAsync(List<int> semesterIds)
        {
            return await _archivedWhitelistDAO.GetBySemesterIdsAsync(semesterIds);
        }
    }
}
