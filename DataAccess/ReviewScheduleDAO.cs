using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class ReviewScheduleDAO : IReviewScheduleDAO
    {
        private readonly FctmsContext _context;

        public ReviewScheduleDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewSchedule>> GetSchedulesByCouncilAsync(int councilId)
        {
            return await _context.ReviewSchedules
                .Include(s => s.SetByLecturer)
                .AsNoTracking()
                .Where(s => s.CouncilId == councilId)
                .OrderBy(s => s.ReviewRound)
                .ToListAsync();
        }
        
        public async Task<ReviewSchedule?> GetScheduleAsync(int councilId, byte round)
        {
            return await _context.ReviewSchedules
                .Include(s => s.SetByLecturer)
                .FirstOrDefaultAsync(s => s.CouncilId == councilId && s.ReviewRound == round);
        }

        public async Task AddOrUpdateScheduleAsync(ReviewSchedule schedule)
        {
            var existing = await _context.ReviewSchedules
                .FirstOrDefaultAsync(s => s.CouncilId == schedule.CouncilId && s.ReviewRound == schedule.ReviewRound);

            if (existing != null)
            {
                existing.ScheduledDate = schedule.ScheduledDate;
                existing.StartTime = schedule.StartTime;
                existing.EndTime = schedule.EndTime;
                existing.MeetLink = schedule.MeetLink;
                existing.NotifiedAt = schedule.NotifiedAt;
                existing.SetByLecturerId = schedule.SetByLecturerId;
                _context.ReviewSchedules.Update(existing);
            }
            else
            {
                await _context.ReviewSchedules.AddAsync(schedule);
            }
            await _context.SaveChangesAsync();
        }
    }
}
