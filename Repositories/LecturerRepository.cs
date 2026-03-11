using BusinessObjects.Models;
using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class LecturerRepository : ILecturerRepository
    {
        private readonly DataAccess.ILecturerDAO _lecturerDAO;

        public LecturerRepository(DataAccess.ILecturerDAO lecturerDAO)
        {
            _lecturerDAO = lecturerDAO;
        }

        public async Task<IEnumerable<Lecturer>> GetAllAsync()
        {
            return await _lecturerDAO.GetAllAsync();
        }

        public async Task<Lecturer?> GetByIdAsync(int id)
        {
            return await _lecturerDAO.GetByIdAsync(id);
        }

        public async Task<Lecturer?> GetByEmailAsync(string email)
        {
            return await _lecturerDAO.GetByEmailAsync(email);
        }

        public async Task<PagedResult<Lecturer>> GetByCampusAsync(string campus, int pageIndex, int pageSize)
        {
            return await _lecturerDAO.GetByCampusAsync(campus, pageIndex, pageSize);
        }

        public async Task<IEnumerable<Lecturer>> GetActiveLecturersAsync()
        {
            return await _lecturerDAO.GetActiveLecturersAsync();
        }

        public async Task AddAsync(Lecturer lecturer)
        {
            await _lecturerDAO.AddAsync(lecturer);
        }

        public async Task UpdateAsync(Lecturer lecturer)
        {
            await _lecturerDAO.UpdateAsync(lecturer);
        }

        public async Task DeleteAsync(Lecturer lecturer)
        {
            await _lecturerDAO.DeleteAsync(lecturer);
        }

        public async Task<IEnumerable<Lecturer>> SearchAsync(string term)
        {
            return await _lecturerDAO.SearchAsync(term);
        }
    }
}
