using BusinessObjects.Models;
using DataAccess;
using System.Threading.Tasks;

namespace Repositories;

public class ThesisReviewRepository : IThesisReviewRepository
{
    private readonly IThesisReviewDAO _thesisReviewDAO;

    public ThesisReviewRepository(IThesisReviewDAO thesisReviewDAO)
    {
        _thesisReviewDAO = thesisReviewDAO;
    }

    public async Task AddOrUpdateReviewAsync(ThesisReview review)
    {
        await _thesisReviewDAO.AddOrUpdateReviewAsync(review);
    }

    public async Task<ThesisReview?> GetReviewByThesisAndReviewerAsync(string thesisId, int reviewerId)
    {
        return await _thesisReviewDAO.GetReviewByThesisAndReviewerAsync(thesisId, reviewerId);
    }
}
