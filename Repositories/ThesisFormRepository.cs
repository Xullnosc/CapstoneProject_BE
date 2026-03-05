using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class ThesisFormRepository : IThesisFormRepository
    {
        private readonly FctmsContext _context;

        public ThesisFormRepository(FctmsContext context)
        {
            _context = context;
        }

        public async Task<ThesisForm?> GetLatestFormAsync()
        {
            return await _context.ThesisForms
                .Include(tf => tf.Histories)
                .Include(tf => tf.Uploader)
                .FirstOrDefaultAsync();
        }

        public async Task<ThesisForm> AddFormAsync(ThesisForm form)
        {
            _context.ThesisForms.Add(form);
            await _context.SaveChangesAsync();
            return form;
        }

        public async Task UpdateFormAsync(ThesisForm form)
        {
            _context.ThesisForms.Update(form);
            await _context.SaveChangesAsync();
        }

        public async Task AddFormHistoryAsync(ThesisFormHistory history)
        {
            _context.ThesisFormHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ThesisFormHistory>> GetFormHistoriesAsync()
        {
            return await _context.ThesisFormHistories
                .Include(h => h.Uploader)
                .OrderByDescending(h => h.VersionNumber)
                .ToListAsync();
        }
    }
}
