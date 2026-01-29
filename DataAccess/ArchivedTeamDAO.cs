using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DataAccess
{
    public class ArchivedTeamDAO : IArchivedTeamDAO
    {
        private readonly FctmsContext _context;

        public ArchivedTeamDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<ArchivedTeam> archivedTeams)
        {
            await _context.ArchivedTeams.AddRangeAsync(archivedTeams);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(ArchivedTeam archivedTeam)
        {
            _context.ArchivedTeams.Add(archivedTeam);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ArchivedTeam>> GetBySemesterIdAsync(int semesterId)
        {
            return await _context.ArchivedTeams
                .Where(x => x.SemesterId == semesterId)
                .Select(t => new ArchivedTeam
                {
                    ArchivedTeamId = t.ArchivedTeamId,
                    OriginalTeamId = t.OriginalTeamId,
                    TeamCode = t.TeamCode,
                    TeamName = t.TeamName,
                    SemesterId = t.SemesterId,
                    LeaderId = t.LeaderId,
                    Status = t.Status,
                    ArchivedAt = t.ArchivedAt
                    // JsonData is excluded for performance
                })
                .ToListAsync();
        }
        public async Task<List<ArchivedTeam>> GetBySemesterIdsAsync(List<int> semesterIds)
        {
            return await _context.ArchivedTeams
                .Where(x => semesterIds.Contains(x.SemesterId))
                .Select(t => new ArchivedTeam
                {
                    ArchivedTeamId = t.ArchivedTeamId,
                    OriginalTeamId = t.OriginalTeamId,
                    TeamCode = t.TeamCode,
                    TeamName = t.TeamName,
                    SemesterId = t.SemesterId,
                    LeaderId = t.LeaderId,
                    Status = t.Status,
                    ArchivedAt = t.ArchivedAt
                    // JsonData excluded
                })
                .ToListAsync();
        }

        public async Task<List<ArchivedTeam>> GetAllAsync()
        {
            return await _context.ArchivedTeams
                .Select(t => new ArchivedTeam
                {
                    ArchivedTeamId = t.ArchivedTeamId,
                    OriginalTeamId = t.OriginalTeamId,
                    TeamCode = t.TeamCode,
                    TeamName = t.TeamName,
                    SemesterId = t.SemesterId,
                    LeaderId = t.LeaderId,
                    Status = t.Status,
                    ArchivedAt = t.ArchivedAt
                    // JsonData is excluded for performance
                })
                .ToListAsync();
        }
    }
}
