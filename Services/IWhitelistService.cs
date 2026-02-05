using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IWhitelistService
    {
        Task<IEnumerable<Whitelist>> GetWhitelistByRoleAsync(int roleId);
    }
}
