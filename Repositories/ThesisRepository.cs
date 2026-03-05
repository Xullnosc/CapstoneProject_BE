using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class ThesisRepository : IThesisRepository
    {
        private readonly IThesisDAO _thesisDAO;

        public ThesisRepository(IThesisDAO thesisDAO)
        {
            _thesisDAO = thesisDAO;
        }

        public Task<Thesis> CreateThesisAsync(Thesis thesis) => _thesisDAO.CreateThesisAsync(thesis);

        public Task<IEnumerable<Thesis>> GetAllThesesAsync() => _thesisDAO.GetAllThesesAsync();

        public Task<Thesis?> GetThesisByIdAsync(string id) => _thesisDAO.GetThesisByIdAsync(id);

        public Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId) => _thesisDAO.GetThesesByUserIdAsync(userId);

        public Task UpdateThesisAsync(Thesis thesis) => _thesisDAO.UpdateThesisAsync(thesis);

        // ─── Phase 02: New Methods ───────────────────────────────────────────────

        public Task<IEnumerable<Thesis>> GetAllThesesFilteredAsync(string? status, int? userId)
            => _thesisDAO.GetAllThesesFilteredAsync(status, userId);

        public Task<Thesis?> GetThesisByIdWithHistoriesAsync(string id)
            => _thesisDAO.GetThesisByIdWithHistoriesAsync(id);

        public Task AddThesisHistoryAsync(ThesisHistory history)
            => _thesisDAO.AddThesisHistoryAsync(history);

        public Task<IEnumerable<Thesis>> GetThesesByUserIdsAsync(IEnumerable<int> userIds, int? semesterId = null)
            => _thesisDAO.GetThesesByUserIdsAsync(userIds, semesterId);

        public Task<Thesis?> GetApprovedThesisByLeaderIdAsync(int leaderId, int? semesterId = null)
            => _thesisDAO.GetApprovedThesisByLeaderIdAsync(leaderId, semesterId);

        public Task<Thesis?> GetThesisForInvitationAsync(int leaderId, int? semesterId = null)
            => _thesisDAO.GetThesisForInvitationAsync(leaderId, semesterId);
    }
}
