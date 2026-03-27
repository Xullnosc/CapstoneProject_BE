using BusinessObjects.DTOs;
using System.Threading.Tasks;

namespace Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDTO> GetDashboardStatsAsync();
    }
}
