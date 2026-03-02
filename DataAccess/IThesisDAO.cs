using BusinessObjects.DTOs;
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
        Task<PagedResult<Thesis>> GetAllThesesAsync(int pageIndex, int pageSize);
        Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId);
        Task<PagedResult<Thesis>> GetThesesByUserIdAsync(int userId, int pageIndex, int pageSize);
        Task UpdateThesisAsync(Thesis thesis);
    }
}
