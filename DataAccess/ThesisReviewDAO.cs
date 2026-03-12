using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DataAccess;

public class ThesisReviewDAO : IThesisReviewDAO
{
    private readonly FctmsContext _context;

    public ThesisReviewDAO(FctmsContext context)
    {
        _context = context;
    }

    public async Task AddOrUpdateReviewAsync(ThesisReview review)
    {
        var existing = await _context.ThesisReviews
            .FirstOrDefaultAsync(r => r.ThesisId == review.ThesisId && r.ReviewerId == review.ReviewerId);

        if (existing == null)
        {
            _context.ThesisReviews.Add(review);
        }
        else
        {
            existing.Status = review.Status;
            existing.Comment = review.Comment;
            if (review.FileUrl != null)
            {
                existing.FileUrl = review.FileUrl;
            }
            existing.ReviewDate = System.DateTime.UtcNow;
            _context.Entry(existing).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<ThesisReview?> GetReviewByThesisAndReviewerAsync(string thesisId, int reviewerId)
    {
        return await _context.ThesisReviews
            .FirstOrDefaultAsync(r => r.ThesisId == thesisId && r.ReviewerId == reviewerId);
    }
}
