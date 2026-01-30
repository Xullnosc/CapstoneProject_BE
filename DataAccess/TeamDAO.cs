using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class TeamDAO : ITeamDAO
    {
        private readonly FctmsContext _context;

        public TeamDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<Team> CreateAsync(Team team)
        {
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task<Team?> GetByIdAsync(int teamId)
        {
            return await _context.Teams
                .Include(t => t.Teammembers)
                .ThenInclude(tm => tm.Student)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);
        }

        public async Task<Team?> GetByCodeAsync(string teamCode)
        {
            return await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamCode == teamCode);
        }

        public async Task<List<Team>> GetBySemesterAsync(int semesterId)
        {
            return await _context.Teams
                .Include(t => t.Teammembers)
                .ThenInclude(tm => tm.Student)
                .Where(t => t.SemesterId == semesterId && t.Status != "Disbanded")
                .ToListAsync();
        }

        public async Task<(List<Team> Items, int TotalCount)> GetBySemesterPagedAsync(int semesterId, int page, int limit)
        {
            var query = _context.Teams
                .Include(t => t.Teammembers)
                .ThenInclude(tm => tm.Student)
                .Where(t => t.SemesterId == semesterId && t.Status != "Disbanded");

            int total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (items, total);
        }

        public async Task<bool> UpdateStatusAsync(int teamId, string status)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return false;

            team.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountTeamsInSemesterAsync(int semesterId)
        {
             return await _context.Teams.CountAsync(t => t.SemesterId == semesterId);
        }

        public async Task<List<string>> GetTeamCodesBySemesterAsync(int semesterId)
        {
            return await _context.Teams
                .Where(t => t.SemesterId == semesterId)
                .Select(t => t.TeamCode)
                .ToListAsync();
        }

        public async Task<Team?> GetTeamByStudentIdAsync(int studentId, int semesterId)
        {
            return await _context.Teams
                .Include(t => t.Teammembers)
                .ThenInclude(tm => tm.Student)
                .Where(t => t.SemesterId == semesterId && t.Status != "Disbanded")
                .FirstOrDefaultAsync(t => t.Teammembers.Any(tm => tm.StudentId == studentId));
        }
        public async Task<bool> UpdateAsync(Team team)
        {
            if (team == null) throw new ArgumentNullException(nameof(team));
            _context.Teams.Update(team);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<Team>> GetForArchivingAsync(int semesterId)
        {
            return await _context.Teams
                .Where(t => t.SemesterId == semesterId)
                .Include(t => t.Teammembers)
                .Include(t => t.Teaminvitations)
                .ToListAsync();
        }

        public async Task DeleteRangeAsync(IEnumerable<Team> teams)
        {
            foreach (var team in teams)
            {
                if (team.Teammembers.Any())
                    _context.Teammembers.RemoveRange(team.Teammembers);
                    
                if (team.Teaminvitations.Any())
                    _context.Teaminvitations.RemoveRange(team.Teaminvitations);
            }
            _context.Teams.RemoveRange(teams);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Team team)
        {
            if (team == null) throw new ArgumentNullException(nameof(team));

            if (team.Teammembers.Any())
                _context.Teammembers.RemoveRange(team.Teammembers);

            if (team.Teaminvitations.Any())
                _context.Teaminvitations.RemoveRange(team.Teaminvitations);

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
    }
}
