using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public class SemesterDAO : ISemesterDAO
    {
        private readonly FctmsContext _context;

        public SemesterDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<List<Semester>> GetAllAsync()
        {
            return await _context.Semesters
                .Include(s => s.Teams)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Semester?> GetByIdAsync(int id)
        {
            return await _context.Semesters
                .Include(s => s.Teams)
                    .ThenInclude(t => t.Teammembers)
                .Include(s => s.Whitelists)
                    .ThenInclude(w => w.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SemesterId == id);
        }

        public async Task<Semester> AddAsync(Semester semester)
        {
            await _context.Semesters.AddAsync(semester);
            await _context.SaveChangesAsync();
            return semester;
        }

        public async Task UpdateAsync(Semester semester)
        {
            _context.Semesters.Update(semester);
            await _context.SaveChangesAsync();
        }



        public async Task<Semester?> GetCurrentSemesterAsync()
        {
            var now = System.DateTime.UtcNow;
            return await _context.Semesters
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now);
        }

        public async Task<Semester?> GetByCodeAsync(string code)
        {
            return await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.SemesterCode == code);
        }
    }
}
