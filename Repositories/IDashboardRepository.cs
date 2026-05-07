using BusinessObjects.DTOs;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardStatsDTO> GetDashboardStatsAsync();

        Task<LecturerDashboardStatsDTO> GetLecturerDashboardStatsAsync(int userId, bool includeReviewerSection);

        /// <summary>Aggregated cross-campus stats for the Admin dashboard.</summary>
        Task<AdminDashboardStatsDTO> GetAdminDashboardStatsAsync();

        Task<HodDashboardStatsDTO> GetHodDashboardStatsAsync();
    }
}
