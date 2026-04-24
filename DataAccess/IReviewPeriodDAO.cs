using BusinessObjects.Models;

namespace DataAccess
{
    public interface IReviewPeriodDAO
    {
        Task<ReviewPeriod?> GetPeriodAsync(int semesterId, byte reviewRound);
        Task<List<ReviewPeriod>> GetPeriodsBySemesterAsync(int semesterId);
        Task AddOrUpdatePeriodAsync(ReviewPeriod period);
    }
}
