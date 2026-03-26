using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class SystemErrorLogRepository : ISystemErrorLogRepository
    {
        private readonly ISystemErrorLogDAO _systemErrorLogDAO;

        public SystemErrorLogRepository(ISystemErrorLogDAO systemErrorLogDAO)
        {
            _systemErrorLogDAO = systemErrorLogDAO;
        }

        public async Task<SystemErrorLog> AddLogAsync(SystemErrorLog log)
        {
            return await _systemErrorLogDAO.AddLogAsync(log);
        }

        public async Task<(IEnumerable<SystemErrorLog> Logs, int TotalCount)> GetLogsAsync(int pageNumber, int pageSize, string? level = null)
        {
            return await _systemErrorLogDAO.GetLogsAsync(pageNumber, pageSize, level);
        }
    }
}
