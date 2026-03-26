using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class SystemErrorLogDAO : ISystemErrorLogDAO
    {
        private readonly FctmsContext _context;

        public SystemErrorLogDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<SystemErrorLog> AddLogAsync(SystemErrorLog log)
        {
            _context.SystemErrorLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<(IEnumerable<SystemErrorLog> Logs, int TotalCount)> GetLogsAsync(int pageNumber, int pageSize, string? level = null)
        {
            var query = _context.SystemErrorLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level) && level != "All")
            {
                query = query.Where(x => x.Level == level);
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }
    }
}
