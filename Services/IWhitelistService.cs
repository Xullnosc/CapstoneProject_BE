using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IWhitelistService
    {
        Task<IEnumerable<Whitelist>> GetWhitelistByRoleAsync(int roleId);
        Task UpdateReviewerStatusAsync(int whitelistId, bool isReviewer);
        Task<Whitelist> AddStudentToWhitelistAsync(Whitelist whitelist);
        Task UpdateWhitelistAsync(Whitelist whitelist);
        Task DeleteWhitelistAsync(int id);
    }
}
