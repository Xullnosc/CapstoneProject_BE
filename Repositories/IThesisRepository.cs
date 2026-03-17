using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IThesisRepository
    {
        Task<Thesis> CreateThesisAsync(Thesis thesis);
        Task<Thesis?> GetThesisByIdAsync(string id);
        Task<IEnumerable<Thesis>> GetAllThesesAsync();
        Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId);
        Task UpdateThesisAsync(Thesis thesis);

        // New methods for Phase 02
        Task<IEnumerable<Thesis>> GetAllThesesFilteredAsync(string? status, int? userId, int? semesterId = null, bool? isLocked = null, bool lecturerOnly = false, int? excludeUserId = null);

        Task<Thesis?> GetThesisByIdWithHistoriesAsync(string id);
        Task AddThesisHistoryAsync(ThesisHistory history);
        Task<IEnumerable<Thesis>> GetThesesByOwnerOrTeamAsync(IEnumerable<int> ownerIds, IEnumerable<int> teamIds, int? semesterId = null);

        // Mentor Invitation Methods
        Task<Thesis?> GetApprovedThesisByLeaderIdAsync(int leaderId, int? semesterId = null);
        Task<Thesis?> GetThesisForInvitationAsync(int leaderId, int? semesterId = null);
    }
}
