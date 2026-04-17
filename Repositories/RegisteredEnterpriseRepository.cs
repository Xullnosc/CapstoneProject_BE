using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class RegisteredEnterpriseRepository : IRegisteredEnterpriseRepository
    {
        private readonly IRegisteredEnterpriseDAO _dao;

        public RegisteredEnterpriseRepository(IRegisteredEnterpriseDAO dao)
        {
            _dao = dao;
        }

        public async Task<IEnumerable<string>> SearchNamesAsync(string query)
        {
            return await _dao.SearchNamesAsync(query);
        }

        public async Task<RegisteredEnterprise?> GetByNameAsync(string name)
        {
            return await _dao.GetByNameAsync(name);
        }

        public async Task<RegisteredEnterprise> AddAsync(RegisteredEnterprise enterprise)
        {
            return await _dao.AddAsync(enterprise);
        }
    }
}
