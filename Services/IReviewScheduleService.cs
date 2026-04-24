using BusinessObjects.DTOs;

namespace Services
{
    public interface IReviewScheduleService
    {
        Task<List<ReviewScheduleDTO>> GetSchedulesByCouncilAsync(int councilId);
        Task<ReviewScheduleDTO> AddOrUpdateScheduleAsync(int councilId, byte reviewRound, DateTime scheduledDate, TimeSpan startTime, TimeSpan endTime, string meetLink, int setByLecturerId);
    }
}
