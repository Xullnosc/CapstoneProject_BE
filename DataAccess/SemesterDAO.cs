using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using BusinessObjects;
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
                .AsNoTracking()
                .OrderBy(s => s.Status == CampusConstants.SemesterStatus.Open ? 0 
                           : s.Status == CampusConstants.SemesterStatus.InProgress ? 1 
                           : s.Status == CampusConstants.SemesterStatus.Upcoming ? 2 
                           : s.Status == CampusConstants.SemesterStatus.Closed ? 10 : 20)
                .ThenByDescending(s => s.StartDate)
                .ToListAsync();
        }

        public async Task<PagedResult<Semester>> GetAllAsync(int pageIndex, int pageSize)
        {
            var query = _context.Semesters
                .Include(s => s.Teams)
                .Include(s => s.Whitelists)
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.Status == CampusConstants.SemesterStatus.Open ? 0 
                           : s.Status == CampusConstants.SemesterStatus.InProgress ? 1 
                           : s.Status == CampusConstants.SemesterStatus.Upcoming ? 2 
                           : s.Status == CampusConstants.SemesterStatus.Closed ? 10 : 20)
                .ThenByDescending(s => s.StartDate)
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
                .Include(s => s.Teams)
                    .ThenInclude(t => t.Leader)
                .Include(s => s.Campus)
                .Include(s => s.Whitelists)
                    .ThenInclude(w => w.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SemesterId == id);
        }

        public async Task<Semester?> GetByIdSimpleAsync(int id)
        {
            return await _context.Semesters
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
            // Use ExecuteUpdateAsync to avoid EF change tracker conflicts
            // (e.g., duplicate Campus tracking when multiple Semester loads share the same DbContext)
            await _context.Semesters
                .Where(s => s.SemesterId == semester.SemesterId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SemesterName, semester.SemesterName)
                    .SetProperty(x => x.SemesterCode, semester.SemesterCode)
                    .SetProperty(x => x.StartDate, semester.StartDate)
                    .SetProperty(x => x.EndDate, semester.EndDate)
                    .SetProperty(x => x.Status, semester.Status)
                    .SetProperty(x => x.CampusId, semester.CampusId));
        }

        public async Task<Semester?> GetCurrentSemesterAsync()
        {
            // Priority 1: Check for any "Live" semester using Stage logic from CampusConstants
            var activeSemester = await _context.Semesters
                .AsNoTracking()
                .FirstOrDefaultAsync(s => 
                    s.Status == CampusConstants.SemesterStatus.Open || 
                    s.Status == CampusConstants.SemesterStatus.InProgress ||
                    s.Status == CampusConstants.SemesterStatus.Active || // Legacy
                    s.Status == CampusConstants.SemesterStatus.ReviewThesis || // Legacy
                    s.Status == CampusConstants.SemesterStatus.ReviewMiddle); // Legacy

            if (activeSemester != null)
            {
                return activeSemester;
            }

            // Priority 2: Fallback to Date Range (Backward Compatibility)
            var now = DateTime.UtcNow;
            return await _context
                .Semesters.AsNoTracking()
                .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now);
        }

        public async Task<bool> HasActiveSemesterAsync()
        {
            // A semester is considered "Active" if it's NOT Closed.
            return await _context.Semesters.AnyAsync(s => s.Status != CampusConstants.SemesterStatus.Closed);
        }

        /// <summary>
        /// Gets a semester by code within the current campus context.
        /// NOTE: This is automatically scoped by the EF Core Global Query Filter on Semesters (CampusId).
        /// Two campuses can therefore have the same SemesterCode without collision.
        /// </summary>
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

        public async Task<PagedResult<Whitelist>> GetOrphanedStudentsAsync(int semesterId, int pageIndex, int pageSize, string? search = null)
        {
            var studentRoleId = await GetStudentRoleIdAsync();

            // Fetch emails of students who are already in a team for this semester
            var teamedEmails = await _context.Teammembers
                .Where(tm => tm.Team.SemesterId == semesterId)
                .Select(tm => tm.Student.Email)
                .Distinct()
                .ToListAsync();

            // Lowercase and trim for insensitive comparison
            var teamedEmailSet = new HashSet<string>(
                teamedEmails.Where(e => !string.IsNullOrEmpty(e)).Select(e => e.Trim()), 
                StringComparer.OrdinalIgnoreCase
            );

            // Filter whitelist students who are NOT in the teamed set
            var query = _context.Whitelists
                .Include(w => w.Role)
                .Where(w => w.SemesterId == semesterId && w.RoleId == studentRoleId)
                .AsNoTracking();

            // We need to filter in memory if we use the HashSet
            var filtered = query.AsEnumerable()
                .Where(w => !string.IsNullOrEmpty(w.Email) && !teamedEmailSet.Contains(w.Email.Trim()));

            // Apply Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                filtered = filtered.Where(w => 
                    (w.Email != null && w.Email.ToLower().Contains(s)) ||
                    (w.FullName != null && w.FullName.ToLower().Contains(s)) ||
                    (w.StudentCode != null && w.StudentCode.ToLower().Contains(s))
                );
            }

            var list = filtered.OrderBy(w => w.FullName ?? w.Email).ToList();
            var totalCount = list.Count;
            var items = list
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<Whitelist>(items, totalCount, pageIndex, pageSize);
        }
    }
}

