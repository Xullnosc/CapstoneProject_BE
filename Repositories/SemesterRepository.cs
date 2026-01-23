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

        public async Task DeleteSemesterAsync(int id) => await _semesterDAO.DeleteAsync(id);

        public async Task<Semester?> GetCurrentSemesterAsync() => await _semesterDAO.GetCurrentSemesterAsync();
    }
}
