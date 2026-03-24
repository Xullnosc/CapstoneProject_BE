using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;

namespace Repositories;

public interface IThesisReviewRepository
{
    Task UpsertReviewerReviewAsync(
        string thesisId,
        int reviewerId,
        string decision,
        string? note,
        string? fileUrl
    );
    Task UpsertHodDecisionAsync(string thesisId, int hodId, string decision, string? comment);

    Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId);
    Task InitializeReviewersAsync(
        string thesisId,
        int reviewer1Id,
        int reviewer2Id,
        int assignedByUserId
    );
    Task<List<ThesisReviewTimelineEventDTO>> GetTimelineAsync(string thesisId);
    Task<ThesisReviewTimelineCommentDTO> AddCommentAsync(
        string thesisId,
        int authorUserId,
        string actorRole,
        CreateThesisReviewCommentDTO dto
    );
}
