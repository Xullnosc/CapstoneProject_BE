using BusinessObjects.DTOs;

namespace Services
{
    public interface IReviewPeriodService
    {
        Task<List<ReviewPeriodDTO>> GetPeriodsBySemesterAsync(int semesterId);
        Task<ReviewPeriodDTO> AddOrUpdatePeriodAsync(int semesterId, byte reviewRound, DateTime startDate, DateTime endDate);
    }
}
