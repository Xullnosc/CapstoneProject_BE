using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using DataAccess;

namespace Repositories;

public class ThesisReviewRepository : IThesisReviewRepository
{
    private readonly IThesisReviewDAO _dao;

    public ThesisReviewRepository(IThesisReviewDAO dao)
    {
        _dao = dao;
    }
    public Task UpsertReviewerReviewAsync(
        string thesisId,
        int reviewerId,
        string decision,
        string? note,
        IEnumerable<int>? checklistIds = null
    ) => _dao.UpsertReviewerReviewAsync(thesisId, reviewerId, decision, note, checklistIds);

    public Task UpsertHodDecisionAsync(
        string thesisId,
        int hodId,
        string decision,
        string? comment,
        IEnumerable<int>? checklistIds = null
    ) => _dao.UpsertHodDecisionAsync(thesisId, hodId, decision, comment, checklistIds);

    public Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId) =>
        _dao.GetReviewStatusAsync(thesisId);

    public Task InitializeReviewersAsync(
        string thesisId,
        int reviewer1Id,
        int reviewer2Id,
        int assignedByUserId
    ) => _dao.InitializeReviewersAsync(thesisId, reviewer1Id, reviewer2Id, assignedByUserId);

    public Task<PagedResult<ThesisReviewTimelineEventDTO>> GetTimelineAsync(
        string thesisId,
        int pageIndex,
        int pageSize
    ) => _dao.GetTimelineAsync(thesisId, pageIndex, pageSize);

    public Task<ThesisReviewTimelineCommentDTO> AddCommentAsync(
        string thesisId,
        int authorUserId,
        string actorRole,
        CreateThesisReviewCommentDTO dto
    ) => _dao.AddCommentAsync(thesisId, authorUserId, actorRole, dto);

    public Task AddRevisionEventAsync(string thesisId, int userId, string? description = null) =>
        _dao.AddRevisionEventAsync(thesisId, userId, description);
}
