using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public class ArchivedWhitelistDAO
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
    }
}
