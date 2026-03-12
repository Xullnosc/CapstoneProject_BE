using BusinessObjects.DTOs;
using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories;

public class ThesisReviewRepository : IThesisReviewRepository
{
    private readonly IThesisReviewDAO _dao;

    public ThesisReviewRepository(IThesisReviewDAO dao)
    {
        _dao = dao;
    }

    public Task AddOrUpdateReviewAsync(ThesisReview review)
        => _dao.AddOrUpdateReviewAsync(review);

    public Task<ThesisReview?> GetReviewByThesisAndReviewerAsync(string thesisId, int reviewerId)
        => _dao.GetReviewByThesisAndReviewerAsync(thesisId, reviewerId);

    public Task<List<ThesisReviewerAssignment>> ReplaceAssignmentsAsync(string thesisId, IEnumerable<int> reviewerIds, int? assignedBy)
        => _dao.ReplaceAssignmentsAsync(thesisId, reviewerIds, assignedBy);

    public Task<List<ThesisReviewerAssignment>> GetAssignmentsAsync(string thesisId)
        => _dao.GetAssignmentsAsync(thesisId);

    public Task<ThesisReview> UpsertReviewerReviewAsync(string thesisId, int reviewerId, string decision, string? note)
        => _dao.UpsertReviewerReviewAsync(thesisId, reviewerId, decision, note);

    public Task<List<ThesisReview>> GetReviewsAsync(string thesisId)
        => _dao.GetReviewsAsync(thesisId);

    public Task<ThesisHodDecision> UpsertHodDecisionAsync(string thesisId, int hodId, string decision, string? note)
        => _dao.UpsertHodDecisionAsync(thesisId, hodId, decision, note);

    public Task<ThesisHodDecision?> GetHodDecisionAsync(string thesisId)
        => _dao.GetHodDecisionAsync(thesisId);

    public Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId)
        => _dao.GetReviewStatusAsync(thesisId);
}
