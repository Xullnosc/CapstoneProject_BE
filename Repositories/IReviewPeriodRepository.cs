using BusinessObjects.Models;

namespace Repositories
{
    public interface IReviewPeriodRepository
    {
        Task<ReviewPeriod?> GetPeriodAsync(int semesterId, byte reviewRound);
        Task<List<ReviewPeriod>> GetPeriodsBySemesterAsync(int semesterId);
        Task AddOrUpdatePeriodAsync(ReviewPeriod period);
    }
}
