using BusinessObjects;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess
{
    public class RegisteredEnterpriseDAO : IRegisteredEnterpriseDAO
    {
        private readonly FctmsContext _context;

        public RegisteredEnterpriseDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<string>> SearchNamesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();

            var normalizedQuery = query.Trim().ToLower();
            return await _context.RegisteredEnterprises
                .AsNoTracking()
                .Where(e => e.EnterpriseName.ToLower().Contains(normalizedQuery))
                .Select(e => e.EnterpriseName)
                .Distinct()
                .ToListAsync();
        }

        public async Task<RegisteredEnterprise?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var normalizedName = name.Trim().ToLower();
            return await _context.RegisteredEnterprises
                .FirstOrDefaultAsync(e => e.EnterpriseName.ToLower() == normalizedName);
        }

        public async Task<RegisteredEnterprise> AddAsync(RegisteredEnterprise enterprise)
        {
            await _context.RegisteredEnterprises.AddAsync(enterprise);
            await _context.SaveChangesAsync();
            return enterprise;
        }
    }
}
