using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;

namespace DataAccess;

public interface IThesisReviewDAO
{
    Task UpsertReviewerReviewAsync(
        string thesisId,
        int reviewerId,
        string decision,
        string? note,
        IEnumerable<int>? checklistIds = null
    );

    Task UpsertHodDecisionAsync(
        string thesisId,
        int hodId,
        string decision,
        string? comment,
        IEnumerable<int>? checklistIds = null
    );

    Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId);
    Task InitializeReviewersAsync(
        string thesisId,
        int reviewer1Id,
        int reviewer2Id,
        int assignedByUserId
    );
    Task<PagedResult<ThesisReviewTimelineEventDTO>> GetTimelineAsync(
        string thesisId,
        int pageIndex,
        int pageSize
    );
    Task<ThesisReviewTimelineCommentDTO> AddCommentAsync(
        string thesisId,
        int authorUserId,
        string actorRole,
        CreateThesisReviewCommentDTO dto
    );

    Task AddRevisionEventAsync(string thesisId, int userId, string? description = null);
}
