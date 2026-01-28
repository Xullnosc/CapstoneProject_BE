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
                .ToListAsync();
        }
    }
}
