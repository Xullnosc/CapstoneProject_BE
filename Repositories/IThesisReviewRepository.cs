using BusinessObjects.DTOs;
using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories;

public interface IThesisReviewRepository
{
    Task<ThesisReview> UpsertReviewerReviewAsync(string thesisId, int reviewerId, string decision, string? note, string? fileUrl);
    Task<List<ThesisReview>> GetReviewsAsync(string thesisId);

    Task<ThesisHodDecision> UpsertHodDecisionAsync(string thesisId, int hodId, string decision, string? comment);
    Task<ThesisHodDecision?> GetHodDecisionAsync(string thesisId);

    Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId);
    Task InitializeReviewersAsync(string thesisId, int reviewer1Id, int reviewer2Id);
}
