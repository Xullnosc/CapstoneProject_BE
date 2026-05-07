using BusinessObjects.DTOs;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IDashboardDAO
    {
        Task<DashboardStatsDTO> GetDashboardStatsAsync();

        Task<LecturerDashboardStatsDTO> GetLecturerDashboardStatsAsync(int userId, bool includeReviewerSection);

        /// <summary>Aggregated cross-campus stats for the Admin dashboard (roles, statuses, trends).</summary>
        Task<AdminDashboardStatsDTO> GetAdminDashboardStatsAsync();
    }
}
