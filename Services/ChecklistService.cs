using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class ChecklistService : IChecklistService
    {
        private readonly IChecklistRepository _repository;
        private readonly IMapper _mapper;

        public ChecklistService(IChecklistRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<ChecklistDTO>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return _mapper.Map<List<ChecklistDTO>>(list);
        }

        public async Task<ChecklistDTO?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ChecklistDTO>(entity);
        }

        public async Task<ChecklistDTO> CreateAsync(ChecklistCreateDTO dto)
        {
            var entity = _mapper.Map<Checklist>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity = await _repository.AddAsync(entity);
            return _mapper.Map<ChecklistDTO>(entity);
        }

        public async Task UpdateAsync(int id, ChecklistUpdateDTO dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"Checklist with id {id} not found.");

            entity.Content = dto.Content;
            entity.DisplayOrder = dto.DisplayOrder;
            await _repository.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"Checklist with id {id} not found.");
            await _repository.DeleteAsync(id);
        }
    }
}
