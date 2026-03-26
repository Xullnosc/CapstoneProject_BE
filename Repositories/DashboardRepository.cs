using BusinessObjects.DTOs;
using DataAccess;
using System.Threading.Tasks;

namespace Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDashboardDAO _dashboardDAO;

        public DashboardRepository(IDashboardDAO dashboardDAO)
        {
            _dashboardDAO = dashboardDAO;
        }

        public async Task<DashboardStatsDTO> GetDashboardStatsAsync()
        {
            return await _dashboardDAO.GetDashboardStatsAsync();
        }
    }
}
