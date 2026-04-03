using BusinessObjects;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess
{
    public class CampusDAO
    {
        private readonly FctmsContext _context;

        public CampusDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<List<Campus>> GetAllAsync()
        {
            return await _context.Campuses
                .Include(c => c.Users.Where(u => u.Role != null && u.Role.RoleName == CampusConstants.Roles.HOD))
                .ToListAsync();
        }

        public async Task<Campus?> GetByIdAsync(int id)
        {
            return await _context.Campuses
                .Include(c => c.Users.Where(u => u.Role != null && u.Role.RoleName == CampusConstants.Roles.HOD))
                .FirstOrDefaultAsync(c => c.CampusId == id);
        }

        public async Task<Campus?> GetByCodeAsync(string code)
        {
            return await _context.Campuses.FirstOrDefaultAsync(c => c.CampusCode == code);
        }

        public async Task<Campus> AddAsync(Campus campus)
        {
            _context.Campuses.Add(campus);
            await _context.SaveChangesAsync();
            return campus;
        }

        public async Task UpdateAsync(Campus campus)
        {
            _context.Campuses.Update(campus);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Campus campus)
        {
            _context.Campuses.Remove(campus);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasActiveReferencesAsync(int campusId)
        {
            bool hasSemesters = await _context.Semesters.AnyAsync(s => s.CampusId == campusId);
            bool hasUsers = await _context.Users.AnyAsync(u => u.CampusId == campusId);
            bool hasTeams = await _context.Teams.AnyAsync(t => t.CampusId == campusId);
            
            return hasSemesters || hasUsers || hasTeams;
        }
    }
}
