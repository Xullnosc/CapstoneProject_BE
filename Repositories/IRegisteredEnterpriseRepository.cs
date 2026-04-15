using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IRegisteredEnterpriseRepository
    {
        Task<IEnumerable<string>> SearchNamesAsync(string query);
        Task<RegisteredEnterprise?> GetByNameAsync(string name);
        Task<RegisteredEnterprise> AddAsync(RegisteredEnterprise enterprise);
    }
}
