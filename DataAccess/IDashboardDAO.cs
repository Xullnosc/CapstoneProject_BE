using BusinessObjects.DTOs;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IDashboardDAO
    {
        Task<DashboardStatsDTO> GetDashboardStatsAsync();

        Task<LecturerDashboardStatsDTO> GetLecturerDashboardStatsAsync(int userId, bool includeReviewerSection);
    }
}
