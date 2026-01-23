using BusinessObjects.Models;
using Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IArchivingService _archivingService;

        public SemesterService(ISemesterRepository semesterRepository, IArchivingService archivingService)
        {
            _semesterRepository = semesterRepository;
            _archivingService = archivingService;
        }

        public async Task<List<Semester>> GetAllSemestersAsync()
        {
            return await _semesterRepository.GetAllSemestersAsync();
        }

        public async Task<Semester?> GetSemesterByIdAsync(int id)
        {
            return await _semesterRepository.GetSemesterByIdAsync(id);
        }

        public async Task<Semester> CreateSemesterAsync(Semester semester)
        {
            // Business logic validation can be added here
            return await _semesterRepository.CreateSemesterAsync(semester);
        }

        public async Task UpdateSemesterAsync(Semester semester)
        {
             // Business logic validation can be added here
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
