using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IArchivedTeamDAO
    {
        Task AddRangeAsync(IEnumerable<ArchivedTeam> archivedTeams);
        Task AddAsync(ArchivedTeam archivedTeam);
        Task<List<ArchivedTeam>> GetBySemesterIdAsync(int semesterId);
    }
}
