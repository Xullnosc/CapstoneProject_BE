using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class CampusService : ICampusService
    {
        private readonly ICampusRepository _campusRepository;

        public CampusService(ICampusRepository campusRepository)
        {
            _campusRepository = campusRepository;
        }

        public async Task<List<CampusDTO>> GetAllCampusesAsync()
        {
            var campuses = await _campusRepository.GetAllAsync();
            return campuses.Select(MapToDTO).ToList();
        }

        public async Task<CampusDTO?> GetCampusByIdAsync(int campusId)
        {
            var campus = await _campusRepository.GetByIdAsync(campusId);
            if (campus == null) return null;
            return MapToDTO(campus);
        }

        public async Task<CampusDTO> CreateCampusAsync(CreateCampusDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CampusCode))
                throw new ArgumentException("Campus code is required.");
            
            if (string.IsNullOrWhiteSpace(dto.CampusName))
                throw new ArgumentException("Campus name is required.");

            var existing = await _campusRepository.GetByCodeAsync(dto.CampusCode.Trim());
            if (existing != null)
                throw new InvalidOperationException($"Campus code '{dto.CampusCode}' already exists.");

            var mappedName = CampusConstants.MapCodeToFullName(dto.CampusCode.Trim()) ?? dto.CampusName.Trim();

            var campus = new Campus
            {
                CampusCode = dto.CampusCode.Trim().ToUpper(),
                CampusName = mappedName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _campusRepository.AddAsync(campus);
            return MapToDTO(campus);
        }

        public async Task<CampusDTO> UpdateCampusAsync(int campusId, UpdateCampusDTO dto)
        {
            var campus = await _campusRepository.GetByIdAsync(campusId);
            if (campus == null)
                throw new KeyNotFoundException($"Campus with ID {campusId} not found.");

            if (!string.IsNullOrWhiteSpace(dto.CampusName))
            {
                campus.CampusName = dto.CampusName.Trim();
            }

            if (dto.IsActive.HasValue)
            {
                if (!dto.IsActive.Value)
                {
                    bool hasReferences = await _campusRepository.HasActiveReferencesAsync(campusId);
                    if (hasReferences)
                    {
                        throw new InvalidOperationException("Cannot deactivate this campus because it has active users, semesters or teams assigned.");
                    }
                }
                
                campus.IsActive = dto.IsActive.Value;
            }

            await _campusRepository.UpdateAsync(campus);
            return MapToDTO(campus);
        }

        public async Task DeleteCampusAsync(int campusId)
        {
            var campus = await _campusRepository.GetByIdAsync(campusId);
            if (campus == null)
                throw new KeyNotFoundException($"Campus with ID {campusId} not found.");

            bool hasReferences = await _campusRepository.HasActiveReferencesAsync(campusId);
            if (hasReferences)
            {
                throw new InvalidOperationException("Cannot delete this campus because it has active references (users, semesters, teams).");
            }

            await _campusRepository.DeleteAsync(campus);
        }

        private static CampusDTO MapToDTO(Campus campus)
        {
            return new CampusDTO
            {
                CampusId = campus.CampusId,
                CampusCode = campus.CampusCode,
                CampusName = campus.CampusName,
                IsActive = campus.IsActive,
                Hods = campus.Users.Select(u => new HodSummaryDTO
                {
                    UserId = u.UserId,
                    FullName = u.FullName ?? string.Empty,
                    Email = u.Email
                }).ToList()
            };
        }
    }
}
