using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IThesisFormDAO
    {
        Task<ThesisForm?> GetLatestFormAsync();
        Task<ThesisForm> AddFormAsync(ThesisForm form);
        Task UpdateFormAsync(ThesisForm form);
        Task AddFormHistoryAsync(ThesisFormHistory history);
        Task<IEnumerable<ThesisFormHistory>> GetFormHistoriesAsync();
    }
}
