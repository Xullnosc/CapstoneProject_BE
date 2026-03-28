using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;

namespace Services
{
    public interface ISystemErrorLogService
    {
        Task<SystemErrorLogDTO> AddLogAsync(SystemErrorLogDTO logDto);
        Task<(IEnumerable<SystemErrorLogDTO> Logs, int TotalCount)> GetLogsAsync(int pageNumber, int pageSize, string? level = null);
    }
}
