using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

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
                .Include(s => s.Whitelists)
                .AsNoTracking().ToListAsync();
        }

        public async Task<Semester?> GetByIdAsync(int id)
        {
            return await _context
                .Semesters.Include(s => s.Teams)
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
            // Priority 1: Check for explicitly ACTIVE semester (The "Golden Rule")
            var activeSemester = await _context.Semesters
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IsActive);

            if (activeSemester != null)
            {
                return activeSemester;
            }

            // Priority 2: Fallback to Date Range (Backward Compatibility)
            // If no semester is flagged IsActive, we fall back to checking dates.
            var now = System.DateTime.UtcNow;
            return await _context
                .Semesters.AsNoTracking()
                .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now);
        }

        public async Task<Semester?> GetByCodeAsync(string code)
        {
            return await _context
                .Semesters.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SemesterCode == code);
        }

        public async Task<int> GetStudentRoleIdAsync()
        {
            var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleName == "Student");
            return role?.RoleId ?? 3; // Fallback to 3 if "Student" role not found (based on migration V2)
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles.AsNoTracking().ToListAsync();
        }

        public async Task<bool> IsOverlapAsync(DateTime start, DateTime end, int? excludeId)
        {
            return await _context.Semesters.AnyAsync(s => 
                (excludeId == null || s.SemesterId != excludeId) &&
                start.Date <= s.EndDate.Date && end.Date >= s.StartDate.Date);
        }
    }
}
