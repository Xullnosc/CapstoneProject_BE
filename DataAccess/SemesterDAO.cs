using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
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

        public async Task<PagedResult<Semester>> GetAllAsync(int pageIndex, int pageSize)
        {
            var query = _context.Semesters
                .Include(s => s.Teams)
                .Include(s => s.Whitelists)
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.SemesterId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Semester>(items, totalCount, pageIndex, pageSize);
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
                .FirstOrDefaultAsync(s => s.Status == "Active");

            if (activeSemester != null)
            {
                return activeSemester;
            }

            // Priority 2: Fallback to Date Range (Backward Compatibility)
            // If no semester is explicitly Active, we fall back to checking which semester includes 'now' in its date range.
            var now = DateTime.UtcNow;
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
            if (role == null)
            {
                throw new InvalidOperationException("Student role not found in the system. Database may be in an inconsistent state.");
            }
            return role.RoleId;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles.AsNoTracking().ToListAsync();
        }

        public async Task<PagedResult<Role>> GetAllRolesAsync(int pageIndex, int pageSize)
        {
            var query = _context.Roles.AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(r => r.RoleId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Role>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<Semester?> IsOverlapAsync(DateTime start, DateTime end, int? excludeId)
        {
            return await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => 
                (excludeId == null || s.SemesterId != excludeId) &&
                start.Date <= s.EndDate.Date && end.Date >= s.StartDate.Date);
        }

        public async Task<List<Whitelist>> GetOrphanedStudentsAsync(int semesterId)
        {
            var studentRoleId = await GetStudentRoleIdAsync();

            // Emails of students who ARE in a team for this semester
            var teamedEmails = await _context.Teammembers
                .Where(tm => tm.Team.SemesterId == semesterId)
                .Select(tm => tm.Student.Email.ToLower())
                .Distinct()
                .ToListAsync();

            var teamedEmailSet = new HashSet<string>(teamedEmails, StringComparer.OrdinalIgnoreCase);

            // Whitelist students for this semester who are NOT in any team
            var allWhitelistStudents = await _context.Whitelists
                .Include(w => w.Role)
                .Where(w => w.SemesterId == semesterId && w.RoleId == studentRoleId)
                .AsNoTracking()
                .ToListAsync();

            return allWhitelistStudents
                .Where(w => !string.IsNullOrEmpty(w.Email) && !teamedEmailSet.Contains(w.Email.Trim()))
                .ToList();
        }
    }
}

