using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class ReviewQuestionDAO : IReviewQuestionDAO
    {
        private readonly FctmsContext _context;

        public ReviewQuestionDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewQuestion>> GetQuestionsAsync(int councilId, byte round)
        {
            return await _context.ReviewQuestions
                .AsNoTracking()
                .Where(q => q.CouncilId == councilId && q.ReviewRound == round)
                .OrderBy(q => q.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<ReviewQuestionResult>> GetResultsAsync(int councilId, byte round, int teamId)
        {
            return await _context.ReviewQuestionResults
                .Include(r => r.Question)
                .AsNoTracking()
                .Where(r => r.Question.CouncilId == councilId && r.ReviewRound == round && r.TeamId == teamId)
                .ToListAsync();
        }

        public async Task AddQuestionAsync(ReviewQuestion question)
        {
            await _context.ReviewQuestions.AddAsync(question);
            await _context.SaveChangesAsync();
        }

        public async Task<ReviewQuestion?> GetQuestionByIdAsync(int id)
        {
            return await _context.ReviewQuestions.FindAsync(id);
        }

        public async Task SaveResultsAsync(List<ReviewQuestionResult> results)
        {
            foreach (var result in results)
            {
                var existing = await _context.ReviewQuestionResults.FirstOrDefaultAsync(
                    r => r.QuestionId == result.QuestionId && 
                         r.TeamId == result.TeamId && 
                         r.ReviewRound == result.ReviewRound && 
                         r.SubmittedBy == result.SubmittedBy);

                if (existing != null)
                {
                    existing.YnValue = result.YnValue;
                    existing.GradeValue = result.GradeValue;
                    existing.SubmittedAt = DateTime.Now;
                    _context.ReviewQuestionResults.Update(existing);
                }
                else
                {
                    result.SubmittedAt = DateTime.Now;
                    await _context.ReviewQuestionResults.AddAsync(result);
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
