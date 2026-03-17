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

    public Task<ThesisReview> UpsertReviewerReviewAsync(string thesisId, int reviewerId, string decision, string? note, string? fileUrl)
        => _dao.UpsertReviewerReviewAsync(thesisId, reviewerId, decision, note, fileUrl);

    public Task<List<ThesisReview>> GetReviewsAsync(string thesisId)
        => _dao.GetReviewsAsync(thesisId);

    public Task<ThesisHodDecision> UpsertHodDecisionAsync(string thesisId, int hodId, string decision, string? comment)
        => _dao.UpsertHodDecisionAsync(thesisId, hodId, decision, comment);

    public Task<ThesisHodDecision?> GetHodDecisionAsync(string thesisId)
        => _dao.GetHodDecisionAsync(thesisId);

    public Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId)
        => _dao.GetReviewStatusAsync(thesisId);

    public Task InitializeReviewersAsync(string thesisId, int reviewer1Id, int reviewer2Id)
        => _dao.InitializeReviewersAsync(thesisId, reviewer1Id, reviewer2Id);
}
