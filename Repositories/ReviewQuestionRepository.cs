using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class ReviewQuestionRepository : IReviewQuestionRepository
    {
        private readonly IReviewQuestionDAO _dao;

        public ReviewQuestionRepository(IReviewQuestionDAO dao)
        {
            _dao = dao;
        }

        public Task<List<ReviewQuestion>> GetQuestionsAsync(int councilId, byte round) => _dao.GetQuestionsAsync(councilId, round);
        public Task<List<ReviewQuestionResult>> GetResultsAsync(int councilId, byte round, int teamId) => _dao.GetResultsAsync(councilId, round, teamId);
        public Task AddQuestionAsync(ReviewQuestion question) => _dao.AddQuestionAsync(question);
        public Task<ReviewQuestion?> GetQuestionByIdAsync(int id) => _dao.GetQuestionByIdAsync(id);
        public Task SaveResultsAsync(List<ReviewQuestionResult> results) => _dao.SaveResultsAsync(results);
    }
}
