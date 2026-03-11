using System.Collections.Generic;
using System.Threading.Tasks;
using Services.DTOs;

namespace Services
{
    public interface IAccessLogService
    {
        Task<(IEnumerable<AccessLogDTO> Logs, int TotalCount)> GetPaginatedLogsAsync(int page, int pageSize);
    }
}
