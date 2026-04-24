using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class ReviewScheduleRepository : IReviewScheduleRepository
    {
        private readonly IReviewScheduleDAO _dao;

        public ReviewScheduleRepository(IReviewScheduleDAO dao)
        {
            _dao = dao;
        }

        public Task<List<ReviewSchedule>> GetSchedulesByCouncilAsync(int councilId) => _dao.GetSchedulesByCouncilAsync(councilId);
        public Task<ReviewSchedule?> GetScheduleAsync(int councilId, byte round) => _dao.GetScheduleAsync(councilId, round);
        public Task AddOrUpdateScheduleAsync(ReviewSchedule schedule) => _dao.AddOrUpdateScheduleAsync(schedule);
    }
}
