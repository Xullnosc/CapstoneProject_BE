using BusinessObjects.Models;
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

        public async Task<List<ArchivedWhitelist>> GetBySemesterIdsAsync(List<int> semesterIds)
        {
            return await _context.ArchivedWhitelists
                .Where(x => semesterIds.Contains(x.SemesterId))
                .ToListAsync();
        }
    }
}
