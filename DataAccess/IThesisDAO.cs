using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IThesisDAO
    {
        Task<Thesis> CreateThesisAsync(Thesis thesis);
        Task<Thesis?> GetThesisByIdAsync(string id);
        Task<IEnumerable<Thesis>> GetAllThesesAsync();
        Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId);
        Task UpdateThesisAsync(Thesis thesis);

        // New methods for Phase 02
        Task<IEnumerable<Thesis>> GetAllThesesFilteredAsync(string? status, int? userId);
        Task<Thesis?> GetThesisByIdWithHistoriesAsync(string id);
        Task AddThesisHistoryAsync(ThesisHistory history);
    }
}
