using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;

namespace Repositories
{
    public interface ISystemErrorLogRepository
    {
        Task<SystemErrorLog> AddLogAsync(SystemErrorLog log);
        Task<(IEnumerable<SystemErrorLog> Logs, int TotalCount)> GetLogsAsync(int pageNumber, int pageSize, string? level = null);
    }
}
