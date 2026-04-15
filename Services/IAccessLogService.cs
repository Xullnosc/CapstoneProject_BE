using System.Collections.Generic;
using System.Threading.Tasks;
using Services.DTOs;
using BusinessObjects.Models;

namespace Services
{
    public interface IAccessLogService
    {
        Task<(IEnumerable<AccessLogDTO> Logs, int TotalCount)> GetPaginatedLogsAsync(int page, int pageSize);
        Task CreateLogAsync(AccessLog log);
    }
}
