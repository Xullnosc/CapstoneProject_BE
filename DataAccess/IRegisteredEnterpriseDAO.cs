using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IRegisteredEnterpriseDAO
    {
        Task<IEnumerable<string>> SearchNamesAsync(string query);
        Task<RegisteredEnterprise?> GetByNameAsync(string name);
        Task<RegisteredEnterprise> AddAsync(RegisteredEnterprise enterprise);
    }
}
