using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess
{
    public class ThesisApplicationDAO : IThesisApplicationDAO
    {
        private readonly FctmsContext _context;

        public ThesisApplicationDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<ThesisApplication> CreateAsync(ThesisApplication app)
        {
            await _context.ThesisApplications.AddAsync(app);
            await _context.SaveChangesAsync();
            return app;
        }

        public async Task<ThesisApplication?> GetByIdAsync(int id)
        {
            return await _context.ThesisApplications
                .Include(a => a.Thesis)
                .Include(a => a.Team)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<ThesisApplication>> GetByTeamIdAsync(int teamId)
        {
            return await _context.ThesisApplications
                .Where(a => a.TeamId == teamId && a.Status != "Cancelled")
                .Include(a => a.Thesis)
                    .ThenInclude(t => t.User)
                .Include(a => a.Team)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ThesisApplication?> GetActiveByThesisAndTeamAsync(string thesisId, int teamId)
        {
            return await _context.ThesisApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.ThesisId == thesisId
                    && a.TeamId == teamId
                    && (a.Status == "Pending" || a.Status == "Approved"));
        }

        public async Task<bool> HasApprovedInSemesterAsync(int teamId, int semesterId)
        {
            return await _context.ThesisApplications
                .AnyAsync(a =>
                    a.TeamId == teamId
                    && a.Status == "Approved"
                    && a.Thesis.SemesterId == semesterId);
        }

        public async Task UpdateAsync(ThesisApplication app)
        {
            _context.ThesisApplications.Update(app);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<ThesisApplication> Items, int TotalCount)> GetByThesisIdPagedAsync(
            string thesisId, string? status, string? search, int page, int limit)
        {
            var query = _context.ThesisApplications
                .Where(a => a.ThesisId == thesisId)
                .Include(a => a.Team)
                    .ThenInclude(t => t.Teammembers)
                        .ThenInclude(m => m.Student)
                .Include(a => a.Team)
                    .ThenInclude(t => t.Leader)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Team.TeamName.Contains(search));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task RejectAllPendingByThesisIdExceptAsync(string thesisId, int exceptId)
        {
            var pendingApps = await _context.ThesisApplications
                .Where(a => a.ThesisId == thesisId && a.Status == "Pending" && a.Id != exceptId)
                .ToListAsync();

            foreach (var app in pendingApps)
            {
                app.Status = "Rejected";
            }

            await _context.SaveChangesAsync();
        }
    }
}
