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

        public async Task<LecturerDashboardStatsDTO> GetLecturerDashboardStatsAsync(
            int userId,
            bool includeReviewerSection
        )
        {
            return await _dashboardRepository.GetLecturerDashboardStatsAsync(userId, includeReviewerSection);
        }

        public async Task<AdminDashboardStatsDTO> GetAdminDashboardStatsAsync()
        {
            return await _dashboardRepository.GetAdminDashboardStatsAsync();
        }

        public async Task<HodDashboardStatsDTO> GetHodDashboardStatsAsync()
        {
            return await _dashboardRepository.GetHodDashboardStatsAsync();
        }
    }
}
