using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class ChecklistRepository : IChecklistRepository
    {
        private readonly IChecklistDAO _dao;

        public ChecklistRepository(IChecklistDAO dao)
        {
            _dao = dao;
        }

        public async Task<List<Checklist>> GetAllAsync() => await _dao.GetAllAsync();

        public async Task<Checklist?> GetByIdAsync(int id) => await _dao.GetByIdAsync(id);

        public async Task<Checklist> AddAsync(Checklist checklist) => await _dao.AddAsync(checklist);

        public async Task UpdateAsync(Checklist checklist) => await _dao.UpdateAsync(checklist);

        public async Task DeleteAsync(int id) => await _dao.DeleteAsync(id);
    }
}
