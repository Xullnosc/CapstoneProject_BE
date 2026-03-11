using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class AccessLogRepository : IAccessLogRepository
    {
        private readonly IAccessLogDAO _accessLogDao;

        public AccessLogRepository(IAccessLogDAO accessLogDao)
        {
            _accessLogDao = accessLogDao;
        }

        public async Task CreateLogAsync(AccessLog log)
        {
            await _accessLogDao.CreateLogAsync(log);
        }

        public async Task<(IEnumerable<AccessLog> Logs, int TotalCount)> GetPaginatedLogsAsync(int page, int pageSize)
        {
            return await _accessLogDao.GetPaginatedLogsAsync(page, pageSize);
        }
    }
}
