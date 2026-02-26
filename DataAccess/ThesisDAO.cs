using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess
{
    public class ThesisDAO : IThesisDAO
    {
        private readonly FctmsContext _context;

        public ThesisDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<Thesis> CreateThesisAsync(Thesis thesis)
        {
            _context.Theses.Add(thesis);
            await _context.SaveChangesAsync();
            return thesis;
        }

        public async Task<IEnumerable<Thesis>> GetAllThesesAsync()
        {
            return await _context.Theses
                .Include(t => t.User)
                .ToListAsync();
        }

        public async Task<Thesis?> GetThesisByIdAsync(string id)
        {
            return await _context.Theses
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.ThesisId == id);
        }

        public async Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId)
        {
            return await _context.Theses
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task UpdateThesisAsync(Thesis thesis)
        {
            _context.Entry(thesis).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
