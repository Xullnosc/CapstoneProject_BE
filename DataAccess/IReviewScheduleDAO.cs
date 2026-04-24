using BusinessObjects.Models;

namespace DataAccess
{
    public interface IReviewScheduleDAO
    {
        Task<List<ReviewSchedule>> GetSchedulesByCouncilAsync(int councilId);
        Task<ReviewSchedule?> GetScheduleAsync(int councilId, byte round);
        Task AddOrUpdateScheduleAsync(ReviewSchedule schedule);
    }
}
