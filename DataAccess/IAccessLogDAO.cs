using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;

namespace DataAccess
{
    public interface IAccessLogDAO
    {
        Task CreateLogAsync(AccessLog log);
        Task<(IEnumerable<AccessLog> Logs, int TotalCount)> GetPaginatedLogsAsync(int page, int pageSize);
    }
}
