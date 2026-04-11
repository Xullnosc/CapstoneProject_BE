using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Repositories;
using Services.DTOs;
using BusinessObjects.Models;

namespace Services
{
    public class AccessLogService : IAccessLogService
    {
        private readonly IAccessLogRepository _accessLogRepository;
        private readonly IMapper _mapper;

        public AccessLogService(IAccessLogRepository accessLogRepository, IMapper mapper)
        {
            _accessLogRepository = accessLogRepository;
            _mapper = mapper;
        }

        public async Task<(IEnumerable<AccessLogDTO> Logs, int TotalCount)> GetPaginatedLogsAsync(int page, int pageSize)
        {
            var (logs, totalCount) = await _accessLogRepository.GetPaginatedLogsAsync(page, pageSize);
            var logDTOs = _mapper.Map<IEnumerable<AccessLogDTO>>(logs);
            return (logDTOs, totalCount);
        }

        public async Task CreateLogAsync(AccessLog log)
        {
            await _accessLogRepository.CreateLogAsync(log);
        }
    }
}
