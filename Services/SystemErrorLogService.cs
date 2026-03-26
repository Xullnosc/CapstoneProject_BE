using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;

namespace Services
{
    public class SystemErrorLogService : ISystemErrorLogService
    {
        private readonly ISystemErrorLogRepository _repository;
        private readonly IMapper _mapper;

        public SystemErrorLogService(ISystemErrorLogRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SystemErrorLogDTO> AddLogAsync(SystemErrorLogDTO logDto)
        {
            var entity = _mapper.Map<SystemErrorLog>(logDto);
            var created = await _repository.AddLogAsync(entity);
            return _mapper.Map<SystemErrorLogDTO>(created);
        }

        public async Task<(IEnumerable<SystemErrorLogDTO> Logs, int TotalCount)> GetLogsAsync(int pageNumber, int pageSize, string? level = null)
        {
            var (logs, totalCount) = await _repository.GetLogsAsync(pageNumber, pageSize, level);
            var dtos = _mapper.Map<IEnumerable<SystemErrorLogDTO>>(logs);
            return (dtos, totalCount);
        }
    }
}
