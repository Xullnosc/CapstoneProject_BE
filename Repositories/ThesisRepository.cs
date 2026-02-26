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
    }
}
