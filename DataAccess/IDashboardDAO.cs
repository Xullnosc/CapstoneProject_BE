using BusinessObjects.DTOs;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IDashboardDAO
    {
        Task<DashboardStatsDTO> GetDashboardStatsAsync();
    }
}
