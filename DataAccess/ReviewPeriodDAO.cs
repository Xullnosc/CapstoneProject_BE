using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class ReviewPeriodDAO : IReviewPeriodDAO
    {
        private readonly FctmsContext _context;

        public ReviewPeriodDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<ReviewPeriod?> GetPeriodAsync(int semesterId, byte reviewRound)
        {
            return await _context.ReviewPeriods
                .AsNoTracking()
                .FirstOrDefaultAsync(rp => rp.SemesterId == semesterId && rp.ReviewRound == reviewRound);
        }

        public async Task<List<ReviewPeriod>> GetPeriodsBySemesterAsync(int semesterId)
        {
            return await _context.ReviewPeriods
                .AsNoTracking()
                .Where(rp => rp.SemesterId == semesterId)
                .OrderBy(rp => rp.ReviewRound)
                .ToListAsync();
        }

        public async Task AddOrUpdatePeriodAsync(ReviewPeriod period)
        {
            var existing = await _context.ReviewPeriods
                .FirstOrDefaultAsync(rp => rp.SemesterId == period.SemesterId && rp.ReviewRound == period.ReviewRound);

            if (existing != null)
            {
                existing.StartDate = period.StartDate;
                existing.EndDate = period.EndDate;
                _context.ReviewPeriods.Update(existing);
            }
            else
            {
                await _context.ReviewPeriods.AddAsync(period);
            }
            await _context.SaveChangesAsync();
        }
    }
}
