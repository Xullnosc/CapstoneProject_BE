using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IArchivedWhitelistDAO
    {
        Task AddRangeAsync(IEnumerable<ArchivedWhitelist> archivedWhitelists);
        Task<List<ArchivedWhitelist>> GetBySemesterIdAsync(int semesterId);
    }
}
