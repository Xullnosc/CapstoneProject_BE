using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class ThesisReviewDAO : IThesisReviewDAO
{
    private readonly FctmsContext _context;

    public ThesisReviewDAO(FctmsContext context)
    {
        _context = context;
    }
    public async Task UpsertReviewerReviewAsync(
        string thesisId,
        int reviewerId,
        string decision,
        string? note,
        IEnumerable<int>? checklistIds = null
    )
    {
        var previous = await GetLatestReviewerDecisionAsync(thesisId, reviewerId);

        var assignedReviewers = await GetActiveAssignedReviewerIdsAsync(thesisId);
        if (!assignedReviewers.Contains(reviewerId))
        {
            if (assignedReviewers.Count >= 2)
            {
                throw new InvalidOperationException(
                    "Two different reviewers have already been assigned for this thesis."
                );
            }

            await CreateEventAsync(
                thesisId,
                "REVIEWER_ASSIGNED",
                reviewerId,
                "REVIEWER",
                null,
                null
            );

            assignedReviewers.Add(reviewerId);
        }

        var reviewEvent = await CreateEventAsync(
            thesisId,
            "REVIEWER_DECISION",
            reviewerId,
            "REVIEWER",
            decision,
            previous
        );

        ThesisReviewComment? decisionComment = null;
        if (!string.IsNullOrWhiteSpace(note))
        {
            decisionComment = new ThesisReviewComment
            {
                EventId = reviewEvent.Id,
                ThesisId = thesisId,
                AuthorUserId = reviewerId,
                Body = note.Trim(),
                CommentType = "DECISION_RATIONALE",
                VisibilityScope = "PUBLIC",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
            };
            _context.ThesisReviewComments.Add(decisionComment);
        }

        await _context.SaveChangesAsync();

        if (checklistIds != null && checklistIds.Any())
        {
            var results = checklistIds.Select(cid => new ThesisReviewChecklistResult
            {
                EventId = reviewEvent.Id,
                ChecklistId = cid,
                IsChecked = true
            });
            _context.ThesisReviewChecklistResults.AddRange(results);
            await _context.SaveChangesAsync();
        }

        await _context.SaveChangesAsync();

        return;
    }
    public async Task UpsertHodDecisionAsync(
        string thesisId,
        int hodId,
        string decision,
        string? note,
        IEnumerable<int>? checklistIds = null
    )
    {
        var previous = await GetLatestHodDecisionAsync(thesisId);

        var hodEvent = await CreateEventAsync(
            thesisId,
            "HOD_FINAL_DECISION",
            hodId,
            "HOD",
            decision,
            previous
        );

        if (!string.IsNullOrWhiteSpace(note))
        {
            _context.ThesisReviewComments.Add(
                new ThesisReviewComment
                {
                    EventId = hodEvent.Id,
                    ThesisId = thesisId,
                    AuthorUserId = hodId,
                    Body = note.Trim(),
                    CommentType = "DECISION_RATIONALE",
                    VisibilityScope = "PUBLIC",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                }
            );
        }

        await _context.SaveChangesAsync();

        if (checklistIds != null && checklistIds.Any())
        {
            var results = checklistIds.Select(cid => new ThesisReviewChecklistResult
            {
                EventId = hodEvent.Id,
                ChecklistId = cid,
                IsChecked = true
            });
            _context.ThesisReviewChecklistResults.AddRange(results);
            await _context.SaveChangesAsync();
        }

        return;
    }

    public async Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId)
    {
        var thesis = await _context
            .Theses.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ThesisId == thesisId);
        if (thesis == null)
            throw new KeyNotFoundException("Thesis not found.");

        var reviewerIds = await GetActiveAssignedReviewerIdsAsync(thesisId);

        var reviewerUids = reviewerIds.Distinct().ToList();
        var relevantUserIds = new List<int>(reviewerUids);

        var latestHodDecisionEvent = await _context
            .ThesisReviewEvents.AsNoTracking()
            .Where(e =>
                e.ThesisId == thesisId
                && e.EventType == "HOD_FINAL_DECISION"
                && e.ActorRole == "HOD"
                && !e.IsDeleted
            )
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync();

        if (latestHodDecisionEvent != null)
            relevantUserIds.Add(latestHodDecisionEvent.ActorUserId);

        var users = await _context
            .Users.AsNoTracking()
            .Where(u => relevantUserIds.Contains(u.UserId))
            .Select(u => new
            {
                u.UserId,
                u.Email,
                u.FullName,
                u.Avatar,
            })
            .ToListAsync();

        var status = new ThesisReviewStatusDTO
        {
            ThesisId = thesisId,
            ThesisStatus = thesis.Status,
            Reviewers = new List<ReviewerProgressDTO>(),
        };

        var decisionEvents = await _context
            .ThesisReviewEvents.AsNoTracking()
            .Where(e =>
                e.ThesisId == thesisId
                && e.EventType == "REVIEWER_DECISION"
                && e.ActorRole == "REVIEWER"
                && !e.IsDeleted
            )
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        var latestByReviewer = decisionEvents
            .GroupBy(e => e.ActorUserId)
            .ToDictionary(g => g.Key, g => g.First());

        if (reviewerIds.Count > 0)
        {
            foreach (var reviewerId in reviewerIds)
            {
                var user = users.FirstOrDefault(x => x.UserId == reviewerId);
                latestByReviewer.TryGetValue(reviewerId, out var latestDecision);
                var latestComment = await _context
                    .ThesisReviewComments.AsNoTracking()
                    .Where(c =>
                        c.EventId == (latestDecision != null ? latestDecision.Id : 0)
                        && !c.IsDeleted
                    )
                    .OrderByDescending(c => c.CreatedAt)
                    .ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                status.Reviewers.Add(
                    new ReviewerProgressDTO
                    {
                        UserId = reviewerId,
                        Email = user?.Email,
                        FullName = user?.FullName,
                        Avatar = user?.Avatar,
                        Decision = latestDecision?.Decision,
                        Comment = latestComment?.Body,
                        ReviewedAt = latestDecision?.CreatedAt,
                    }
                );
            }
        }

        // Compute overall status logic
        var decided = status.Reviewers.Where(x => !string.IsNullOrWhiteSpace(x.Decision)).ToList();

        if (reviewerIds.Distinct().Count() < 2)
        {
            status.OverallStatus = "Pending";
            status.RequiresHodDecision = false;
        }
        else if (decided.Count < 2)
        {
            status.OverallStatus = "Pending";
            status.RequiresHodDecision = false;
        }
        else
        {
            var passCount = decided.Count(x =>
                string.Equals(x.Decision, "Pass", StringComparison.OrdinalIgnoreCase)
            );
            if (passCount == 2)
            {
                status.OverallStatus = "Pass";
            }
            else if (passCount == 0)
            {
                status.OverallStatus = "Fail";
            }
            else
            {
                status.OverallStatus = "Split";
                status.RequiresHodDecision = true;
            }
        }

        if (latestHodDecisionEvent != null)
        {
            var u = users.FirstOrDefault(x => x.UserId == latestHodDecisionEvent.ActorUserId);

            var latestHodComment = await _context
                .ThesisReviewComments.AsNoTracking()
                .Where(c => c.EventId == latestHodDecisionEvent.Id && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            status.HodDecision = new HodDecisionDTO
            {
                HodId = latestHodDecisionEvent.ActorUserId,
                Email = u?.Email,
                FullName = u?.FullName,
                Avatar = u?.Avatar,
                Decision = latestHodDecisionEvent.Decision ?? "Fail",
                Comment = latestHodComment?.Body,
                DecidedAt = latestHodDecisionEvent.CreatedAt,
            };
            status.OverallStatus = "HodDecided";
            status.RequiresHodDecision = false;
        }

        return status;
    }

    public async Task InitializeReviewersAsync(
        string thesisId,
        int reviewer1Id,
        int reviewer2Id,
        int assignedByUserId
    )
    {
        var priorAssignments = await _context
            .ThesisReviewEvents.Where(e =>
                e.ThesisId == thesisId && e.EventType == "REVIEWER_ASSIGNED" && !e.IsDeleted
            )
            .ToListAsync();

        foreach (var assignment in priorAssignments)
        {
            assignment.IsDeleted = true;
            assignment.UpdatedAt = DateTime.UtcNow;
            assignment.UpdatedBy = assignedByUserId;
        }

        await _context.SaveChangesAsync();

        await CreateEventAsync(thesisId, "REVIEWER_ASSIGNED", reviewer1Id, "REVIEWER", null, null);

        await CreateEventAsync(thesisId, "REVIEWER_ASSIGNED", reviewer2Id, "REVIEWER", null, null);

        var previousHodDecisions = await _context
            .ThesisReviewEvents.Where(e =>
                e.ThesisId == thesisId && e.EventType == "HOD_FINAL_DECISION" && !e.IsDeleted
            )
            .ToListAsync();
        foreach (var hodDecision in previousHodDecisions)
        {
            hodDecision.IsDeleted = true;
            hodDecision.UpdatedAt = DateTime.UtcNow;
            hodDecision.UpdatedBy = assignedByUserId;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<ThesisReviewTimelineEventDTO>> GetTimelineAsync(string thesisId)
    {
        var thesisExists = await _context
            .Theses.AsNoTracking()
            .AnyAsync(t => t.ThesisId == thesisId);
        if (!thesisExists)
        {
            throw new KeyNotFoundException("Thesis not found.");
        }

        var events = await _context
            .ThesisReviewEvents.AsNoTracking()
            .Where(e => e.ThesisId == thesisId && !e.IsDeleted)
            .Join(
                _context.Users.AsNoTracking(),
                e => e.ActorUserId,
                u => u.UserId,
                (e, u) =>
                    new ThesisReviewTimelineEventDTO
                    {
                        EventId = e.Id,
                        ThesisId = e.ThesisId,
                        EventType = e.EventType,
                        ActorUserId = e.ActorUserId,
                        ActorRole = e.ActorRole,
                        ActorName = u.FullName,
                        ActorEmail = u.Email,
                        ActorAvatar = u.Avatar,
                        Decision = e.Decision,
                        CreatedAt = e.CreatedAt,
                    }
            )
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.EventId)
            .ToListAsync();

        if (events.Count == 0)
        {
            return events;
        }

        var eventIds = events.Select(e => e.EventId).ToList();
        var comments = await _context
            .ThesisReviewComments.AsNoTracking()
            .Where(c => c.ThesisId == thesisId && eventIds.Contains(c.EventId) && !c.IsDeleted)
            .Join(
                _context.Users.AsNoTracking(),
                c => c.AuthorUserId,
                u => u.UserId,
                (c, u) =>
                    new ThesisReviewTimelineCommentDTO
                    {
                        Id = c.Id,
                        EventId = c.EventId,
                        ParentCommentId = c.ParentCommentId,
                        AuthorUserId = c.AuthorUserId,
                        AuthorName = u.FullName,
                        AuthorEmail = u.Email,
                        AuthorAvatar = u.Avatar,
                        Body = c.Body,
                        CommentType = c.CommentType,
                        VisibilityScope = c.VisibilityScope,
                        CreatedAt = c.CreatedAt,
                    }
            )
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync();

        var commentById = comments.ToDictionary(c => c.Id);
        var eventTopLevel = events.ToDictionary(
            e => e.EventId,
            _ => new List<ThesisReviewTimelineCommentDTO>()
        );

        foreach (var comment in comments)
        {
            if (
                comment.ParentCommentId.HasValue
                && commentById.TryGetValue(comment.ParentCommentId.Value, out var parent)
            )
            {
                parent.Replies.Add(comment);
            }
            else if (eventTopLevel.TryGetValue(comment.EventId, out var bucket))
            {
                bucket.Add(comment);
            }
        }

        foreach (var evt in events)
        {
            evt.Comments = eventTopLevel[evt.EventId];
        }

        // Populate Checklist Results
        var checklistResults = await _context.ThesisReviewChecklistResults
            .AsNoTracking()
            .Where(r => eventIds.Contains(r.EventId) && r.IsChecked)
            .Join(_context.Checklists.AsNoTracking(),
                r => r.ChecklistId,
                c => c.ChecklistId,
                (r, c) => new { r.EventId, c.Content })
            .ToListAsync();

        foreach (var result in checklistResults)
        {
            var evt = events.FirstOrDefault(e => e.EventId == result.EventId);
            if (evt != null)
            {
                evt.ChecklistResults.Add(result.Content);
            }
        }

        return events;
    }

    public async Task<ThesisReviewTimelineCommentDTO> AddCommentAsync(
        string thesisId,
        int authorUserId,
        string actorRole,
        CreateThesisReviewCommentDTO dto
    )
    {
        var thesisExists = await _context
            .Theses.AsNoTracking()
            .AnyAsync(t => t.ThesisId == thesisId);
        if (!thesisExists)
        {
            throw new KeyNotFoundException("Thesis not found.");
        }

        if (string.IsNullOrWhiteSpace(dto.Body))
        {
            throw new ArgumentException("Comment body is required.");
        }

        long targetEventId;
        long? parentCommentId = dto.ParentCommentId;

        if (parentCommentId.HasValue)
        {
            var parent = await _context
                .ThesisReviewComments.AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == parentCommentId.Value && c.ThesisId == thesisId && !c.IsDeleted
                );
            if (parent == null)
            {
                throw new KeyNotFoundException("Parent comment not found.");
            }
            targetEventId = parent.EventId;
        }
        else if (dto.EventId.HasValue)
        {
            var eventExists = await _context
                .ThesisReviewEvents.AsNoTracking()
                .AnyAsync(e => e.Id == dto.EventId.Value && e.ThesisId == thesisId && !e.IsDeleted);
            if (!eventExists)
            {
                throw new KeyNotFoundException("Target event not found.");
            }
            targetEventId = dto.EventId.Value;
        }
        else
        {
            var createdEvent = await CreateEventAsync(
                thesisId,
                "COMMENT_ADDED",
                authorUserId,
                actorRole,
                null,
                null
            );
            targetEventId = createdEvent.Id;
        }

        var commentType = string.IsNullOrWhiteSpace(dto.CommentType)
            ? (parentCommentId.HasValue ? "REPLY" : "FOLLOW_UP")
            : dto.CommentType.Trim().ToUpperInvariant();
        var visibility = string.IsNullOrWhiteSpace(dto.VisibilityScope)
            ? "PUBLIC"
            : dto.VisibilityScope.Trim().ToUpperInvariant();

        var comment = new ThesisReviewComment
        {
            EventId = targetEventId,
            ThesisId = thesisId,
            ParentCommentId = parentCommentId,
            AuthorUserId = authorUserId,
            Body = dto.Body.Trim(),
            CommentType = commentType,
            VisibilityScope = visibility,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };

        _context.ThesisReviewComments.Add(comment);
        await _context.SaveChangesAsync();

        var user = await _context
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == authorUserId);
        return new ThesisReviewTimelineCommentDTO
        {
            Id = comment.Id,
            EventId = comment.EventId,
            ParentCommentId = comment.ParentCommentId,
            AuthorUserId = comment.AuthorUserId,
            AuthorName = user?.FullName,
            AuthorEmail = user?.Email,
            AuthorAvatar = user?.Avatar,
            Body = comment.Body,
            CommentType = comment.CommentType,
            VisibilityScope = comment.VisibilityScope,
            CreatedAt = comment.CreatedAt,
        };
    }

    private async Task<ThesisReviewEvent> CreateEventAsync(
        string thesisId,
        string eventType,
        int actorUserId,
        string actorRole,
        string? decision,
        string? previousDecision
    )
    {
        var nextSequence =
            (
                await _context
                    .ThesisReviewEvents.AsNoTracking()
                    .Where(e => e.ThesisId == thesisId)
                    .MaxAsync(e => (int?)e.SequenceNo)
                ?? 0
            ) + 1;

        var created = new ThesisReviewEvent
        {
            ThesisId = thesisId,
            EventType = eventType,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Decision = decision,
            PreviousDecision = previousDecision,
            SequenceNo = nextSequence,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };

        _context.ThesisReviewEvents.Add(created);
        await _context.SaveChangesAsync();
        return created;
    }

    private async Task<string?> GetLatestReviewerDecisionAsync(string thesisId, int reviewerId)
    {
        return await _context
            .ThesisReviewEvents.AsNoTracking()
            .Where(e =>
                e.ThesisId == thesisId
                && e.ActorUserId == reviewerId
                && e.EventType == "REVIEWER_DECISION"
                && !e.IsDeleted
            )
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Select(e => e.Decision)
            .FirstOrDefaultAsync();
    }

    private async Task<List<int>> GetActiveAssignedReviewerIdsAsync(string thesisId)
    {
        return await _context
            .ThesisReviewEvents.AsNoTracking()
            .Where(e =>
                e.ThesisId == thesisId
                && e.EventType == "REVIEWER_ASSIGNED"
                && e.ActorRole == "REVIEWER"
                && !e.IsDeleted
            )
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Select(e => e.ActorUserId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<string?> GetLatestHodDecisionAsync(string thesisId)
    {
        return await _context
            .ThesisReviewEvents.AsNoTracking()
            .Where(e =>
                e.ThesisId == thesisId
                && e.EventType == "HOD_FINAL_DECISION"
                && e.ActorRole == "HOD"
                && !e.IsDeleted
            )
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Select(e => e.Decision)
            .FirstOrDefaultAsync();
    }
}
