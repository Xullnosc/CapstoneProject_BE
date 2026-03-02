using BusinessObjects.Models;
using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IArchivedWhitelistDAO
    {
        Task AddRangeAsync(IEnumerable<ArchivedWhitelist> archivedWhitelists);
        Task<List<ArchivedWhitelist>> GetBySemesterIdAsync(int semesterId);
        Task <PagedResult<ArchivedWhitelist>> GetBySemesterIdAsync(int semesterId, int pageIndex, int limit);
        Task<List<ArchivedWhitelist>> GetBySemesterIdsAsync(List<int> semesterIds);
        Task <PagedResult<ArchivedWhitelist>> GetBySemesterIdsAsync(List<int> semesterId, int pageIndex, int limit);
    }
}
