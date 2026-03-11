using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Models;

namespace DataAccess
{
    public class AccessLogDAO : IAccessLogDAO
    {
        private readonly FctmsContext _context;

        public AccessLogDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task CreateLogAsync(AccessLog log)
        {
            _context.AccessLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<AccessLog> Logs, int TotalCount)> GetPaginatedLogsAsync(int page, int pageSize)
        {
            var query = _context.AccessLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt);

            var totalCount = await query.CountAsync();
            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }
    }
}
