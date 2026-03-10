using BusinessObjects.DTOs;
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
                .AsNoTracking()
                .Include(t => t.User)
                .ToListAsync();
        }

        public async Task<PagedResult<Thesis>> GetAllThesesAsync(int pageIndex, int pageSize)
        {
            var query = _context.Theses
                .AsNoTracking()
                .Include(t => t.User);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(t => t.ThesisId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Thesis>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<Thesis?> GetThesisByIdAsync(string id)
        {
            return await _context.Theses
                .AsNoTracking()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.ThesisId == id);
        }

        public async Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId)
        {
            return await _context.Theses
                .AsNoTracking()
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task<PagedResult<Thesis>> GetThesesByUserIdAsync(int userId, int pageIndex, int pageSize)
        {
            var query = _context.Theses
                .AsNoTracking()
                .Include(t => t.User)
                .Where(t => t.UserId == userId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(t => t.ThesisId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Thesis>(items, totalCount, pageIndex, pageSize);
        }

        public async Task UpdateThesisAsync(Thesis thesis)
        {
            _context.Entry(thesis).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // ─── Phase 02: New Methods ───────────────────────────────────────────────

        public async Task<IEnumerable<Thesis>> GetAllThesesFilteredAsync(string? status, int? userId, int? semesterId = null)
        {
            var query = _context.Theses
                .AsNoTracking()
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status == status);

            if (userId.HasValue)
                query = query.Where(t => t.UserId == userId.Value);

            if (semesterId.HasValue)
                query = query.Where(t => t.SemesterId == semesterId.Value);

            return await query.OrderByDescending(t => t.UpdateDate).ToListAsync();
        }

        public async Task<Thesis?> GetThesisByIdWithHistoriesAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var thesis = await _context.Theses
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.ThesisHistories) // Explicitly included
                .FirstOrDefaultAsync(t => t.ThesisId == id);

            if (thesis != null && thesis.ThesisHistories != null)
            {
                // EF Core cannot sort inside Include -> sort in memory after loading
                thesis.ThesisHistories = thesis.ThesisHistories
                    .OrderByDescending(h => h.VersionNumber)
                    .ToList();
            }

            return thesis;
        }

        public async Task AddThesisHistoryAsync(ThesisHistory history)
        {
            _context.ThesisHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Thesis>> GetThesesByUserIdsAsync(IEnumerable<int> userIds, int? semesterId = null)
        {
            var query = _context.Theses
                .AsNoTracking()
                .Include(t => t.User)
                .Where(t => userIds.Contains(t.UserId));

            if (semesterId.HasValue)
            {
                query = query.Where(t => t.SemesterId == semesterId.Value);
            }

            return await query.OrderByDescending(t => t.UpdateDate).ToListAsync();
        }

        public async Task<Thesis?> GetApprovedThesisByLeaderIdAsync(int leaderId, int? semesterId = null)
        {
            var query = _context.Theses
                .AsNoTracking()
                .Where(t => t.UserId == leaderId && 
                           (t.Status == "Approved" || t.Status == "Published"));

            if (semesterId.HasValue)
            {
                query = query.Where(t => t.SemesterId == semesterId.Value);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<Thesis?> GetThesisForInvitationAsync(int leaderId, int? semesterId = null)
        {
            var query = _context.Theses
                .AsNoTracking()
                .Where(t => t.UserId == leaderId && 
                           (t.Status == "On Mentor Inviting" || t.Status == "Approved" || t.Status == "Published"));

            if (semesterId.HasValue)
            {
                query = query.Where(t => t.SemesterId == semesterId.Value);
            }

            return await query.FirstOrDefaultAsync();
        }
    }
}
