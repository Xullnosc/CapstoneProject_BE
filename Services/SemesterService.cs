using BusinessObjects.DTOs;
using BusinessObjects.Models;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using Repositories;

namespace Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IArchivingService _archivingService;
        private readonly IMapper _mapper;

        public SemesterService(ISemesterRepository semesterRepository, IArchivingService archivingService, IMapper mapper)
        {
            _semesterRepository = semesterRepository;
            _archivingService = archivingService;
            _mapper = mapper;
        }

        public async Task<List<SemesterDTO>> GetAllSemestersAsync()
        {
            var semesters = await _semesterRepository.GetAllSemestersAsync();
            return _mapper.Map<List<SemesterDTO>>(semesters);
        }

        public async Task<SemesterDTO?> GetSemesterByIdAsync(int id)
        {
            var semester = await _semesterRepository.GetSemesterByIdAsync(id);
            return _mapper.Map<SemesterDTO>(semester);
        }

        public async Task<SemesterDTO> CreateSemesterAsync(SemesterCreateDTO semesterCreateDTO)
        {
            var semester = _mapper.Map<Semester>(semesterCreateDTO);
            var createdSemester = await _semesterRepository.CreateSemesterAsync(semester);
            return _mapper.Map<SemesterDTO>(createdSemester);
        }

        public async Task UpdateSemesterAsync(SemesterCreateDTO semesterCreateDTO)
        {
            var semester = _mapper.Map<Semester>(semesterCreateDTO);
            await _semesterRepository.UpdateSemesterAsync(semester);
        }

        public async Task DeleteSemesterAsync(int id)
        {
            await _semesterRepository.DeleteSemesterAsync(id);
        }

        public async Task EndSemesterAsync(int id)
        {
            var semester = await _semesterRepository.GetSemesterByIdAsync(id);
            if (semester == null)
            {
                throw new KeyNotFoundException($"Semester with ID {id} not found.");
            }

            // 1. Mark as Inactive
            semester.IsActive = false;
            await _semesterRepository.UpdateSemesterAsync(semester);

            // 2. Archive Data
            await _archivingService.ArchiveSemesterAsync(id);
        }
    }
}
