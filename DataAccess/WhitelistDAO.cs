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
                .Include(w => w.Role)
                .FirstOrDefaultAsync(w => w.Email == email);
        }

        public async Task<List<Whitelist>> GetBySemesterIdAsync(int semesterId)
        {
            return await _context.Whitelists
                .Where(w => w.SemesterId == semesterId)
                .ToListAsync();
        }

        public async Task DeleteRangeAsync(IEnumerable<Whitelist> whitelists)
        {
            _context.Whitelists.RemoveRange(whitelists);
            await _context.SaveChangesAsync();
        }
    }
}
