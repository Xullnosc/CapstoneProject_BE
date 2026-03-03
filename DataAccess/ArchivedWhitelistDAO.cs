using BusinessObjects.Models;
using BusinessObjects.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public class ArchivedWhitelistDAO : IArchivedWhitelistDAO
    {
        private readonly FctmsContext _context;

        public ArchivedWhitelistDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<ArchivedWhitelist> archivedWhitelists)
        {
            await _context.ArchivedWhitelists.AddRangeAsync(archivedWhitelists);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ArchivedWhitelist>> GetBySemesterIdAsync(int semesterId)
        {
            return await _context.ArchivedWhitelists
                .Where(x => x.SemesterId == semesterId)
                .ToListAsync();
        }

        public async Task <PagedResult<ArchivedWhitelist>> GetBySemesterIdAsync(int semesterId, int pageIndex, int limit)
        {
            if (pageIndex <= 0)
                pageIndex = 1;
            if (limit <= 0)
                limit = 10;
            var baseQuery = _context.ArchivedWhitelists
                .AsNoTracking()
                .Where(x => x.SemesterId == semesterId);
            var totalCountTask = baseQuery.CountAsync();
            var itemsTask = baseQuery
                .OrderBy(x => x.ArchivedWhitelistId) // indexed column preferred
                .Skip((pageIndex - 1) * limit)
                .Take(limit)
                .ToListAsync();
            await Task.WhenAll(totalCountTask, itemsTask);
            return new PagedResult<ArchivedWhitelist>(itemsTask.Result, totalCountTask.Result, pageIndex, limit);
        }

        public async Task<List<ArchivedWhitelist>> GetBySemesterIdsAsync(List<int> semesterIds)
        {
            return await _context.ArchivedWhitelists
                .Where(x => semesterIds.Contains(x.SemesterId))
                .ToListAsync();
        }

        public async Task<PagedResult<ArchivedWhitelist>> GetBySemesterIdsAsync(List<int> semesterIds, int pageIndex, int limit)
        {
            if (pageIndex <= 0)
                pageIndex = 1;
            if (limit <= 0)
                limit = 10;
            var baseQuery = _context.ArchivedWhitelists
                .AsNoTracking()
                .Where(x => semesterIds.Contains(x.SemesterId));
            var totalCountTask = baseQuery.CountAsync();
            var itemsTask = baseQuery
                .OrderBy(x => x.ArchivedWhitelistId) // indexed column preferred
                .Skip((pageIndex - 1) * limit)
                .Take(limit)
                .ToListAsync();
            await Task.WhenAll(totalCountTask, itemsTask);
            return new PagedResult<ArchivedWhitelist>(itemsTask.Result, totalCountTask.Result, pageIndex, limit);
        }

    }
}
