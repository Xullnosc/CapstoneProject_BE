using BusinessObjects.Models;

namespace Repositories
{
    public interface IReviewScheduleRepository
    {
        Task<List<ReviewSchedule>> GetSchedulesByCouncilAsync(int councilId);
        Task<ReviewSchedule?> GetScheduleAsync(int councilId, byte round);
        Task AddOrUpdateScheduleAsync(ReviewSchedule schedule);
    }
}
