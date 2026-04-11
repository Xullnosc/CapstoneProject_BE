using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

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
            return await _context.Theses.AsNoTracking().Include(t => t.User).ToListAsync();
        }

        public async Task<PagedResult<Thesis>> GetAllThesesAsync(int pageIndex, int pageSize)
        {
            var query = _context.Theses.AsNoTracking().Include(t => t.User);

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
            return await _context
                .Theses.AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.Team)
                .Include(t => t.Mentor1)
                .Include(t => t.Mentor2)
                .FirstOrDefaultAsync(t => t.ThesisId == id);
        }

        public async Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId)
        {
            return await _context
                .Theses.AsNoTracking()
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task<PagedResult<Thesis>> GetThesesByUserIdAsync(
            int userId,
            int pageIndex,
            int pageSize
        )
        {
            var query = _context
                .Theses.AsNoTracking()
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
            var tracked = _context.Theses.Local.FirstOrDefault(t => t.ThesisId == thesis.ThesisId);
            if (tracked != null && tracked != thesis)
            {
                _context.Entry(tracked).CurrentValues.SetValues(thesis);
            }
            else
            {
                _context.Entry(thesis).State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();
        }

        // ─── Phase 02: New Methods ───────────────────────────────────────────────

        public async Task<IEnumerable<Thesis>> GetAllThesesFilteredAsync(
            string? status,
            int? userId,
            int? teamId = null,
            int? semesterId = null,
            bool? isLocked = null,
            bool lecturerOnly = false,
            int? excludeUserId = null
        )
        {
            var query = _context.Theses.AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.Mentor1)
                .Include(t => t.Mentor2)
                .AsQueryable();
 
            if (!string.IsNullOrEmpty(status))
            {
                if (string.Equals(status, "Verified", StringComparison.OrdinalIgnoreCase))
                {
                    var verifiedStatuses = new[] { "Published", "Need Update" };
                    query = query.Where(t => verifiedStatuses.Contains(t.Status));
                }
                else
                {
                    query = query.Where(t => t.Status == status);
                }
            }
 
            if (userId.HasValue)
            {
                query = query.Where(t => t.UserId == userId.Value);
            }
 
            if (teamId.HasValue)
            {
                query = query.Where(t => t.TeamId == teamId.Value);
            }
            if (semesterId.HasValue)
                query = query.Where(t => t.SemesterId == semesterId.Value);

            if (isLocked.HasValue)
                query = query.Where(t => t.IsLocked == isLocked.Value);

            if (excludeUserId.HasValue)
                query = query.Where(t => @t.UserId != excludeUserId.Value);

            if (lecturerOnly)
                query = query.Where(t => t.User.Role != null && t.User.Role.RoleName == "Lecturer");

            return await query.OrderByDescending(t => t.UpdateDate).ToListAsync();
        }

        public async Task<IEnumerable<Thesis>> GetThesesForEvaluationExportAsync()
        {
            // Semester filtering is done in-memory by the service using the loaded Team navigation.
            return await _context.Theses
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(t => t.Status != "Cancelled")
                .Include(t => t.Team)
                    .ThenInclude(team => team.Teammembers)
                        .ThenInclude(teamMember => teamMember.Student)
                            .ThenInclude(student => student.AccountDetail)
                .Include(t => t.Mentor1)
                .Include(t => t.Mentor2)
                .Include(t => t.ThesisReviewEvents.Where(e => !e.IsDeleted))
                    .ThenInclude(e => e.ActorUser)
                .Include(t => t.ThesisReviewEvents.Where(e => !e.IsDeleted))
                    .ThenInclude(e => e.Comments.Where(c => !c.IsDeleted))
                        .ThenInclude(c => c.AuthorUser)
                .Include(t => t.ThesisReviewEvents.Where(e => !e.IsDeleted))
                    .ThenInclude(e => e.ChecklistResults)
                        .ThenInclude(cr => cr.Checklist)
                .OrderBy(t => t.Team == null ? 1 : 0)
                .ThenBy(t => t.Team!.TeamCode)
                .ThenBy(t => t.ThesisId)
                .ToListAsync();
        }

        public async Task<Thesis?> GetThesisByIdWithHistoriesAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            var thesis = await _context
                .Theses.Include(t => t.User)
                .Include(t => t.Mentor1)
                .Include(t => t.Mentor2)
                .Include(t => t.Team)
                .Include(t => t.ThesisHistories)
                    .ThenInclude(h => h.UploadedByUser)
                .FirstOrDefaultAsync(t => t.ThesisId == id);

            if (thesis != null && thesis.ThesisHistories != null)
            {
                // EF Core cannot sort inside Include -> sort in memory after loading
                thesis.ThesisHistories = thesis
                    .ThesisHistories.OrderByDescending(h => h.VersionNumber)
                    .ToList();
            }

            return thesis;
        }

        public async Task AddThesisHistoryAsync(ThesisHistory history)
        {
            _context.ThesisHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Thesis>> GetThesesByOwnerOrTeamAsync(
            IEnumerable<int> ownerIds,
            IEnumerable<int> teamIds,
            int? semesterId = null
        )
        {
            var query = _context
                .Theses.AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.Mentor1)
                .Include(t => t.Mentor2)
                .Where(t =>
                    (t.UserId.HasValue && ownerIds.Contains(t.UserId.Value))
                    || (t.TeamId.HasValue && teamIds.Contains(t.TeamId.Value))
                    || (t.MentorId1.HasValue && ownerIds.Contains(t.MentorId1.Value))
                    || (t.MentorId2.HasValue && ownerIds.Contains(t.MentorId2.Value))
                );

            if (semesterId.HasValue)
            {
                query = query.Where(t => t.SemesterId == semesterId.Value);
            }

            return await query.OrderByDescending(t => t.UpdateDate).ToListAsync();
        }

        public async Task<Thesis?> GetApprovedThesisByLeaderIdAsync(
            int leaderId,
            int? semesterId = null
        )
        {
            var query = _context
                .Theses.AsNoTracking()
                .Where(t =>
                    t.UserId == leaderId && (t.Status == "Approved" || t.Status == "Published")
                );

            if (semesterId.HasValue)
            {
                query = query.Where(t => t.SemesterId == semesterId.Value);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<Thesis?> GetThesisForInvitationAsync(int leaderId, int teamId, int? semesterId = null)
        {
            var query = _context
                .Theses.AsNoTracking()
                .Where(t =>
                    (t.UserId == leaderId || (t.TeamId.HasValue && t.TeamId == teamId))
                    && (
                        t.Status == "On Mentor Inviting"
                        || t.Status == "Registered"
                        || t.Status == "Reviewing"
                        || t.Status == "Need Update"
                    )
                );
 
            if (semesterId.HasValue)
            {
                query = query.Where(t => t.SemesterId == semesterId.Value);
            }
 
            return await query.FirstOrDefaultAsync();
        }
 
        public async Task<IEnumerable<Thesis>> GetThesesByTeamIdAsync(int teamId)
        {
            return await _context.Theses
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.Mentor1)
                .Include(t => t.Mentor2)
                .Where(t => t.TeamId == teamId)
                .ToListAsync();
        }
    }
}
