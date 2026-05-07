using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace DataAccess
{
    public interface IThesisDAO
    {
        Task<Thesis> CreateThesisAsync(Thesis thesis);
        Task<Thesis?> GetThesisByIdAsync(string id);
        Task<IEnumerable<Thesis>> GetAllThesesAsync();
        Task<PagedResult<Thesis>> GetAllThesesAsync(int pageIndex, int pageSize);
        Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId);
        Task<PagedResult<Thesis>> GetThesesByUserIdAsync(int userId, int pageIndex, int pageSize);
        Task UpdateThesisAsync(Thesis thesis);

        // New methods for Phase 02
        Task<IEnumerable<Thesis>> GetAllThesesFilteredAsync(string? status, int? userId, int? teamId = null, int? semesterId = null, bool? isLocked = null, bool lecturerOnly = false, int? excludeUserId = null);
         Task<IEnumerable<Thesis>> GetThesesForEvaluationExportAsync();
        Task<Thesis?> GetThesisByIdWithHistoriesAsync(string id);
        Task AddThesisHistoryAsync(ThesisHistory history);
        Task<IEnumerable<Thesis>> GetThesesByOwnerOrTeamAsync(
            IEnumerable<int> ownerIds,
            IEnumerable<int> teamIds,
            int? semesterId = null
        );

        // Duplication pipeline
        Task<IEnumerable<Thesis>> GetThesesBySemesterIdsAsync(IEnumerable<int> semesterIds, IEnumerable<string>? statuses = null);

        // Mentor Invitation Methods
        Task<Thesis?> GetApprovedThesisByLeaderIdAsync(int leaderId, int? semesterId = null);
        Task<Thesis?> GetThesisForInvitationAsync(int leaderId, int teamId, int? semesterId = null);
        Task<IEnumerable<Thesis>> GetThesesByTeamIdAsync(int teamId);
    }
}
