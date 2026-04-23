using BusinessObjects.Models;
using BusinessObjects.DTOs;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class SemesterRepository : ISemesterRepository
    {
        private readonly ISemesterDAO _semesterDAO;

        public SemesterRepository(ISemesterDAO semesterDAO)
        {
            _semesterDAO = semesterDAO;
        }

        public async Task<List<Semester>> GetAllSemestersAsync() => await _semesterDAO.GetAllAsync();

        public async Task<PagedResult<Semester>> GetAllSemestersAsync(int pageIndex, int pageSize) 
            => await _semesterDAO.GetAllAsync(pageIndex, pageSize);

        public async Task<Semester?> GetSemesterByIdAsync(int id) => await _semesterDAO.GetByIdAsync(id);
        public async Task<Semester?> GetSemesterByIdSimpleAsync(int id) => await _semesterDAO.GetByIdSimpleAsync(id);

        public async Task<Semester> CreateSemesterAsync(Semester semester) => await _semesterDAO.AddAsync(semester);

        public async Task UpdateSemesterAsync(Semester semester) => await _semesterDAO.UpdateAsync(semester);
        public async Task UpdateMidtermReviewAsync(int semesterId, DateTime lockDate) => await _semesterDAO.UpdateMidtermReviewAsync(semesterId, lockDate);



        public async Task<Semester?> GetCurrentSemesterAsync() => await _semesterDAO.GetCurrentSemesterAsync();

        public async Task<Semester?> GetSemesterByCodeAsync(string code) => await _semesterDAO.GetByCodeAsync(code);

        public async Task<int> GetStudentRoleIdAsync() => await _semesterDAO.GetStudentRoleIdAsync();

        public async Task<List<Role>> GetAllRolesAsync() => await _semesterDAO.GetAllRolesAsync();

        public async Task<Semester?> IsOverlapAsync(DateTime start, DateTime end, int? excludeId) 
            => await _semesterDAO.IsOverlapAsync(start, end, excludeId);
        
        public async Task<bool> HasActiveSemesterAsync() => await _semesterDAO.HasActiveSemesterAsync();

        public async Task<bool> SemesterExistsAsync(int semesterId)
        {
            var semester = await _semesterDAO.GetByIdAsync(semesterId);
            return semester != null;
        }

        public async Task<PagedResult<Whitelist>> GetOrphanedStudentsAsync(int semesterId, int pageIndex, int pageSize, string? search = null)
            => await _semesterDAO.GetOrphanedStudentsAsync(semesterId, pageIndex, pageSize, search);

        public async Task<List<Semester>> GetPreviousClosedSemestersAsync(int currentSemesterId, int count)
            => await _semesterDAO.GetPreviousClosedSemestersAsync(currentSemesterId, count);
    }
}
