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
    }
}
