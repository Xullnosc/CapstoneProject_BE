using BusinessObjects.Models;
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

        public async Task<Semester?> GetSemesterByIdAsync(int id) => await _semesterDAO.GetByIdAsync(id);

        public async Task<Semester> CreateSemesterAsync(Semester semester) => await _semesterDAO.AddAsync(semester);

        public async Task UpdateSemesterAsync(Semester semester) => await _semesterDAO.UpdateAsync(semester);



        public async Task<Semester?> GetCurrentSemesterAsync() => await _semesterDAO.GetCurrentSemesterAsync();

        public async Task<Semester?> GetSemesterByCodeAsync(string code) => await _semesterDAO.GetByCodeAsync(code);

        public async Task<int> GetStudentRoleIdAsync() => await _semesterDAO.GetStudentRoleIdAsync();

        public async Task<List<Role>> GetAllRolesAsync() => await _semesterDAO.GetAllRolesAsync();

        public async Task<bool> IsOverlapAsync(DateTime start, DateTime end, int? excludeId) 
            => await _semesterDAO.IsOverlapAsync(start, end, excludeId);
    }
}
