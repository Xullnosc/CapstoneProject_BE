using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;

namespace Services
{
    public class SystemParameterService : ISystemParameterService
    {
        private readonly ISystemParameterRepository _repository;

        public SystemParameterService(ISystemParameterRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SystemParameterDTO>> GetAllParametersAsync()
        {
            var parameters = await _repository.GetAllParametersAsync();
            return parameters.Select(p => new SystemParameterDTO
            {
                Key = p.Key,
                Value = p.Value,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();
        }

        public async Task<SystemParameterDTO?> GetParameterByKeyAsync(string key)
        {
            var parameter = await _repository.GetParameterByKeyAsync(key);
            if (parameter == null) return null;

            return new SystemParameterDTO
            {
                Key = parameter.Key,
                Value = parameter.Value,
                Description = parameter.Description,
                CreatedAt = parameter.CreatedAt,
                UpdatedAt = parameter.UpdatedAt
            };
        }

        public async Task UpdateParameterAsync(SystemParameterDTO parameterDto)
        {
            var existingParam = await _repository.GetParameterByKeyAsync(parameterDto.Key);
            if (existingParam != null)
            {
                existingParam.Value = parameterDto.Value;
                if (!string.IsNullOrEmpty(parameterDto.Description))
                {
                    existingParam.Description = parameterDto.Description;
                }
                
                await _repository.UpdateParameterAsync(existingParam);
            }
        }
    }
}
