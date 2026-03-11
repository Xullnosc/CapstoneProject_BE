using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;

namespace Repositories
{
    public interface IAccessLogRepository
    {
        Task CreateLogAsync(AccessLog log);
        Task<(IEnumerable<AccessLog> Logs, int TotalCount)> GetPaginatedLogsAsync(int page, int pageSize);
    }
}
