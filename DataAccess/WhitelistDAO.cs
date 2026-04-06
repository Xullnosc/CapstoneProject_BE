using System.Linq;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class WhitelistDAO : IWhitelistDAO
    {
        private readonly FctmsContext _context;

        public WhitelistDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<Whitelist?> GetByEmailAsync(string email)
        {
            return await _context.Whitelists
                .AsNoTracking()
                .Include(w => w.Role)
                .Include(w => w.Semester)
                .Where(w => w.Email == email)
                .OrderByDescending(w => w.Semester != null && w.Semester.Status == CampusConstants.SemesterStatus.Open)
                .ThenByDescending(w => w.SemesterId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Whitelist>> GetBySemesterIdAsync(int semesterId)
        {
            return await _context.Whitelists
                .Include(w => w.Role)
                .Where(w => w.SemesterId == semesterId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PagedResult<Whitelist>> GetBySemesterIdAsync(int semesterId, int pageIndex, int pageSize)
        {
            var query = _context.Whitelists
                .Where(w => w.SemesterId == semesterId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(w => w.WhitelistId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Whitelist>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<List<Whitelist>> GetByRoleAsync(int roleId)
        {
            return await _context.Whitelists
                .Where(w => w.RoleId == roleId)
                .ToListAsync();
        }

        public async Task<PagedResult<Whitelist>> GetByRoleAsync(int roleId, int pageIndex, int pageSize)
        {
            var query = _context.Whitelists
                .Where(w => w.RoleId == roleId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(w => w.WhitelistId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Whitelist>(items, totalCount, pageIndex, pageSize);
        }

        public async Task DeleteRangeAsync(IEnumerable<Whitelist> whitelists)
        {
            _context.Whitelists.RemoveRange(whitelists);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Whitelist> whitelists)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Whitelists.AddRangeAsync(whitelists);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task ReplaceStudentsBySemesterAsync(int semesterId, int studentRoleId, IEnumerable<Whitelist> newStudents)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingStudents = await _context.Whitelists
                    .Where(w => w.SemesterId == semesterId && w.RoleId == studentRoleId)
                    .ToListAsync();

                if (existingStudents.Any())
                {
                    _context.Whitelists.RemoveRange(existingStudents);
                }

                var studentsToAdd = newStudents.ToList();
                if (studentsToAdd.Any())
                {
                    await _context.Whitelists.AddRangeAsync(studentsToAdd);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<Whitelist?> GetByIdAsync(int id)
        {
            return await _context.Whitelists.FindAsync(id);
        }

        public async Task UpdateAsync(Whitelist whitelist)
        {
            _context.Whitelists.Update(whitelist);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(Whitelist whitelist)
        {
            await _context.Whitelists.AddAsync(whitelist);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Whitelist whitelist)
        {
            _context.Whitelists.Remove(whitelist);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Whitelist>> SearchAsync(string term, int semesterId)
        {
            return await _context.Whitelists
                .Where(w => w.SemesterId == semesterId &&
                            (w.FullName.Contains(term) || w.Email.Contains(term) || w.StudentCode.Contains(term)))
                .Take(10)
                .ToListAsync();
        }

        public async Task<bool> IsWhitelistedInSemesterAsync(string email, int semesterId)
        {
            return await _context.Whitelists
                .AnyAsync(w => w.Email == email && w.SemesterId == semesterId);
        }
    }
}
