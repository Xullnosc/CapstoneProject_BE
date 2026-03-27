using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DataAccess
{
    public class DashboardDAO : IDashboardDAO
    {
        private readonly FctmsContext _context;

        public DashboardDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDTO> GetDashboardStatsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalTheses = await _context.Theses.CountAsync();
            var totalTeams = await _context.Teams.CountAsync();
            var totalSemesters = await _context.Semesters.CountAsync();

            return new DashboardStatsDTO
            {
                TotalUsers = totalUsers,
                TotalTheses = totalTheses,
                TotalTeams = totalTeams,
                TotalSemesters = totalSemesters
            };
        }
    }
}
