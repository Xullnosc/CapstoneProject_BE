using BusinessObjects.Models;
using System.Threading.Tasks;

namespace Repositories;

public interface IThesisReviewRepository
{
    Task AddOrUpdateReviewAsync(ThesisReview review);
    Task<ThesisReview?> GetReviewByThesisAndReviewerAsync(string thesisId, int reviewerId);
}
