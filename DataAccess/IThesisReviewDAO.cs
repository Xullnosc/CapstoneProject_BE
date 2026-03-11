using BusinessObjects.Models;
using System.Threading.Tasks;

namespace DataAccess;

public interface IThesisReviewDAO
{
    Task AddOrUpdateReviewAsync(ThesisReview review);
    Task<ThesisReview?> GetReviewByThesisAndReviewerAsync(string thesisId, int reviewerId);
}
