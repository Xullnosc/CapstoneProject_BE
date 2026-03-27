using BusinessObjects.DTOs;
using Repositories;
using System.Threading.Tasks;

namespace Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<DashboardStatsDTO> GetDashboardStatsAsync()
        {
            return await _dashboardRepository.GetDashboardStatsAsync();
        }
    }
}
