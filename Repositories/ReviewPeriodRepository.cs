using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class ReviewPeriodRepository : IReviewPeriodRepository
    {
        private readonly IReviewPeriodDAO _dao;

        public ReviewPeriodRepository(IReviewPeriodDAO dao)
        {
            _dao = dao;
        }

        public Task<ReviewPeriod?> GetPeriodAsync(int semesterId, byte reviewRound) => _dao.GetPeriodAsync(semesterId, reviewRound);
        public Task<List<ReviewPeriod>> GetPeriodsBySemesterAsync(int semesterId) => _dao.GetPeriodsBySemesterAsync(semesterId);
        public Task AddOrUpdatePeriodAsync(ReviewPeriod period) => _dao.AddOrUpdatePeriodAsync(period);
    }
}
