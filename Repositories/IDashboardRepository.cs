using BusinessObjects.DTOs;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardStatsDTO> GetDashboardStatsAsync();

        Task<LecturerDashboardStatsDTO> GetLecturerDashboardStatsAsync(int userId, bool includeReviewerSection);
    }
}
