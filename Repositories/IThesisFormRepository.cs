using BusinessObjects.Models;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IThesisFormRepository
    {
        Task<ThesisForm?> GetLatestFormAsync();
        Task<ThesisForm> AddFormAsync(ThesisForm form);
        Task UpdateFormAsync(ThesisForm form);
        Task AddFormHistoryAsync(ThesisFormHistory history);
        Task<IEnumerable<ThesisFormHistory>> GetFormHistoriesAsync();
    }
}
