using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;

namespace DataAccess
{
    public interface ISystemErrorLogDAO
    {
        Task<SystemErrorLog> AddLogAsync(SystemErrorLog log);
        Task<(IEnumerable<SystemErrorLog> Logs, int TotalCount)> GetLogsAsync(int pageNumber, int pageSize, string? level = null);
    }
}
