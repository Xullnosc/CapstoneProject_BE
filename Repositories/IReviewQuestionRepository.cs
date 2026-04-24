using BusinessObjects.Models;

namespace Repositories
{
    public interface IReviewQuestionRepository
    {
        Task<List<ReviewQuestion>> GetQuestionsAsync(int councilId, byte round);
        Task<List<ReviewQuestionResult>> GetResultsAsync(int councilId, byte round, int teamId);
        Task AddQuestionAsync(ReviewQuestion question);
        Task<ReviewQuestion?> GetQuestionByIdAsync(int id);
        Task SaveResultsAsync(List<ReviewQuestionResult> results);
    }
}
