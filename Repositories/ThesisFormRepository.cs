using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class ThesisFormRepository : IThesisFormRepository
    {
        private readonly DataAccess.IThesisFormDAO _thesisFormDAO;

        public ThesisFormRepository(DataAccess.IThesisFormDAO thesisFormDAO)
        {
            _thesisFormDAO = thesisFormDAO;
        }

        public async Task<ThesisForm?> GetLatestFormAsync()
        {
            return await _thesisFormDAO.GetLatestFormAsync();
        }

        public async Task<ThesisForm> AddFormAsync(ThesisForm form)
        {
            return await _thesisFormDAO.AddFormAsync(form);
        }

        public async Task UpdateFormAsync(ThesisForm form)
        {
            await _thesisFormDAO.UpdateFormAsync(form);
        }

        public async Task AddFormHistoryAsync(ThesisFormHistory history)
        {
            await _thesisFormDAO.AddFormHistoryAsync(history);
        }

        public async Task<IEnumerable<ThesisFormHistory>> GetFormHistoriesAsync()
        {
            return await _thesisFormDAO.GetFormHistoriesAsync();
        }
    }
}
