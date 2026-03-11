using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess
{
    public class ThesisFormDAO : IThesisFormDAO
    {
        private readonly FctmsContext _context;

        public ThesisFormDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<ThesisForm?> GetLatestFormAsync()
        {
            return await _context.ThesisForms
                .AsNoTracking()
                .Include(tf => tf.ThesisFormHistories)
                .Include(tf => tf.UploadedByNavigation)
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
                .AsNoTracking()
                .Include(h => h.UploadedByNavigation)
                .OrderByDescending(h => h.VersionNumber)
                .ToListAsync();
        }
    }
}
