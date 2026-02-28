using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public class ChecklistDAO : IChecklistDAO
    {
        private readonly FctmsContext _context;

        public ChecklistDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<List<Checklist>> GetAllAsync()
        {
            return await _context.Checklists
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.ChecklistId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Checklist?> GetByIdAsync(int id)
        {
            return await _context.Checklists
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChecklistId == id);
        }

        public async Task<Checklist> AddAsync(Checklist checklist)
        {
            await _context.Checklists.AddAsync(checklist);
            await _context.SaveChangesAsync();
            return checklist;
        }

        public async Task UpdateAsync(Checklist checklist)
        {
            _context.Checklists.Update(checklist);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Checklists.FindAsync(id);
            if (entity != null)
            {
                _context.Checklists.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
