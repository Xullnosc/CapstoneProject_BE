using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class LecturerDAO : ILecturerDAO
    {
        private readonly FctmsContext _context;

        public LecturerDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Lecturer>> GetAllAsync()
        {
            var list = await _context.Lecturers
                .Include(l => l.CampusNavigation)
                .AsNoTracking()
                .OrderBy(l => l.FullName)
                .ToListAsync();
            list.ForEach(l => l.Campus = CampusConstants.MapIdToCode(l.CampusId));
            return list;
        }

        public async Task<Lecturer?> GetByIdAsync(int id)
        {
            var l = await _context.Lecturers
                .Include(l => l.CampusNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LecturerId == id);
            if (l != null) l.Campus = CampusConstants.MapIdToCode(l.CampusId);
            return l;
        }

        public async Task<Lecturer?> GetByEmailAsync(string email)
        {
            var l = await _context.Lecturers
                .Include(l => l.CampusNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Email == email);
            if (l != null) l.Campus = CampusConstants.MapIdToCode(l.CampusId);
            return l;
        }

        public async Task<PagedResult<Lecturer>> GetByCampusAsync(
            string campus,
            int pageIndex,
            int pageSize
        )
        {
            string? mappedCampus = CampusConstants.MapCodeToFullName(campus)?.Trim();
            if (pageIndex <= 0)
            {
                pageIndex = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
            }
            if (pageSize > 100)
            {
                pageSize = 100;
            }

            int? campusId = CampusConstants.MapToId(campus);
            if (!campusId.HasValue)
            {
                return new PagedResult<Lecturer>(new List<Lecturer>(), 0, pageIndex, pageSize);
            }

            var baseQuery = _context.Lecturers
                .Include(l => l.CampusNavigation)
                .AsNoTracking()
                .Where(l => l.IsActive && l.CampusId == campusId.Value)
                .OrderBy(l => l.FullName);

            var totalCountTask = baseQuery.CountAsync();
            var itemsTask = baseQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            await Task.WhenAll(totalCountTask, itemsTask);

            var result = itemsTask.Result;
            result.ForEach(l => l.Campus = CampusConstants.MapIdToCode(l.CampusId));

            return new PagedResult<Lecturer>(result, totalCountTask.Result, pageIndex, pageSize);
        }

        public async Task<IEnumerable<Lecturer>> GetActiveLecturersAsync()
        {
            return await _context
                .Lecturers.AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lecturer>> GetReviewersAsync()
        {
            return await _context
                .Lecturers.AsNoTracking()
                .Where(l => l.IsReviewer && !string.IsNullOrWhiteSpace(l.Email))
                .OrderBy(l => l.FullName)
                .ThenBy(l => l.Email)
                .ToListAsync();
        }

        public async Task AddAsync(Lecturer lecturer)
        {
            await _context.Lecturers.AddAsync(lecturer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Lecturer lecturer)
        {
            _context.Lecturers.Update(lecturer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Lecturer lecturer)
        {
            _context.Lecturers.Remove(lecturer);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Lecturer>> SearchAsync(string term)
        {
            string normalizedTerm = term?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(term))
            {
                return await _context
                    .Lecturers.AsNoTracking()
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.FullName)
                    .Take(20)
                    .ToListAsync();
            }

            var list = await _context.Lecturers
                .Include(l => l.CampusNavigation)
                .AsNoTracking()
                .Where(l => l.IsActive && ((l.FullName ?? string.Empty).Contains(normalizedTerm) || l.Email.Contains(normalizedTerm)))
                .OrderBy(l => l.FullName)
                .ToListAsync();
            list.ForEach(l => l.Campus = CampusConstants.MapIdToCode(l.CampusId));
            return list;
        }

        public async Task<IEnumerable<Lecturer>> GetByEmailsAsync(List<string> emails)
        {
            if (emails == null || !emails.Any())
                return new List<Lecturer>();
            var lowerEmails = emails.Select(e => e.ToLower().Trim()).ToList();
            return await _context
                .Lecturers.Where(l => l.Email != null && lowerEmails.Contains(l.Email.ToLower()))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
