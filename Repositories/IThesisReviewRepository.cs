using BusinessObjects.DTOs;
using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories;

public interface IThesisReviewRepository
{
    Task<List<ThesisReviewerAssignment>> ReplaceAssignmentsAsync(string thesisId, IEnumerable<int> reviewerIds, int? assignedBy);
    Task<List<ThesisReviewerAssignment>> GetAssignmentsAsync(string thesisId);

    Task<ThesisReview> UpsertReviewerReviewAsync(string thesisId, int reviewerId, string decision, string? note);
    Task<List<ThesisReview>> GetReviewsAsync(string thesisId);

    Task<ThesisHodDecision> UpsertHodDecisionAsync(string thesisId, int hodId, string decision, string? note);
    Task<ThesisHodDecision?> GetHodDecisionAsync(string thesisId);

    Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId);
}

