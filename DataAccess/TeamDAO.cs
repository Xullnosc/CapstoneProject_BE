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
    }
}
