using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class CampusRepository : ICampusRepository
    {
        private readonly CampusDAO _campusDao;

        public CampusRepository(CampusDAO campusDao)
        {
            _campusDao = campusDao;
        }

        public Task<List<Campus>> GetAllAsync() => _campusDao.GetAllAsync();
        public Task<Campus?> GetByIdAsync(int id) => _campusDao.GetByIdAsync(id);
        public Task<Campus?> GetByCodeAsync(string code) => _campusDao.GetByCodeAsync(code);
        public Task<Campus> AddAsync(Campus campus) => _campusDao.AddAsync(campus);
        public Task UpdateAsync(Campus campus) => _campusDao.UpdateAsync(campus);
        public Task DeleteAsync(Campus campus) => _campusDao.DeleteAsync(campus);
        public Task<bool> HasActiveReferencesAsync(int campusId) => _campusDao.HasActiveReferencesAsync(campusId);
    }
}
