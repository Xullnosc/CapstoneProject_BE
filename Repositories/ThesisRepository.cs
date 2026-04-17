using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class ThesisRepository : IThesisRepository
    {
        private readonly IThesisDAO _thesisDAO;

        public ThesisRepository(IThesisDAO thesisDAO)
        {
            _thesisDAO = thesisDAO;
        }

        public Task<Thesis> CreateThesisAsync(Thesis thesis) =>
            _thesisDAO.CreateThesisAsync(thesis);

        public Task<IEnumerable<Thesis>> GetAllThesesAsync() => _thesisDAO.GetAllThesesAsync();

        public Task<Thesis?> GetThesisByIdAsync(string id) => _thesisDAO.GetThesisByIdAsync(id);

        public Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId) =>
            _thesisDAO.GetThesesByUserIdAsync(userId);

        public Task UpdateThesisAsync(Thesis thesis) => _thesisDAO.UpdateThesisAsync(thesis);

        // ─── Phase 02: New Methods ───────────────────────────────────────────────

        public Task<IEnumerable<Thesis>> GetAllThesesFilteredAsync(
            string? status,
            int? userId,
            int? teamId = null,
            int? semesterId = null,
            bool? isLocked = null,
            bool lecturerOnly = false,
            int? excludeUserId = null
        ) =>
            _thesisDAO.GetAllThesesFilteredAsync(
                status,
                userId,
                teamId,
                semesterId,
                isLocked,
                lecturerOnly,
                excludeUserId
            );

        public Task<IEnumerable<Thesis>> GetThesesForEvaluationExportAsync()
            => _thesisDAO.GetThesesForEvaluationExportAsync();

        public Task<Thesis?> GetThesisByIdWithHistoriesAsync(string id) =>
            _thesisDAO.GetThesisByIdWithHistoriesAsync(id);

        public Task AddThesisHistoryAsync(ThesisHistory history) =>
            _thesisDAO.AddThesisHistoryAsync(history);

        public Task<IEnumerable<Thesis>> GetThesesByOwnerOrTeamAsync(
            IEnumerable<int> ownerIds,
            IEnumerable<int> teamIds,
            int? semesterId = null
        ) => _thesisDAO.GetThesesByOwnerOrTeamAsync(ownerIds, teamIds, semesterId);

        public Task<Thesis?> GetThesisForInvitationAsync(int leaderId, int teamId, int? semesterId = null)
            => _thesisDAO.GetThesisForInvitationAsync(leaderId, teamId, semesterId);
        
        public Task<IEnumerable<Thesis>> GetThesesByTeamIdAsync(int teamId)
            => _thesisDAO.GetThesesByTeamIdAsync(teamId);
        
        public Task<Thesis?> GetApprovedThesisByLeaderIdAsync(
            int leaderId,
            int? semesterId = null
        ) => _thesisDAO.GetApprovedThesisByLeaderIdAsync(leaderId, semesterId);

        public Task<IEnumerable<Thesis>> GetThesesBySemesterIdsAsync(IEnumerable<int> semesterIds)
            => _thesisDAO.GetThesesBySemesterIdsAsync(semesterIds);
    }
}
