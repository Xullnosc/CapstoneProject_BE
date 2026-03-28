using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs;

public class ThesisReviewTimelineEventDTO
{
    public long EventId { get; set; }
    public string ThesisId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public int ActorUserId { get; set; }
    public string ActorRole { get; set; } = null!;
    public string? ActorName { get; set; }
    public string? ActorEmail { get; set; }
    public string? ActorAvatar { get; set; }
    public string? Decision { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> ChecklistResults { get; set; } = [];
    public List<ThesisReviewTimelineCommentDTO> Comments { get; set; } = [];
}

public class ThesisReviewTimelineCommentDTO
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public long? ParentCommentId { get; set; }
    public int AuthorUserId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public string? AuthorAvatar { get; set; }
    public string Body { get; set; } = null!;
    public string CommentType { get; set; } = null!;
    public string VisibilityScope { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<ThesisReviewTimelineCommentDTO> Replies { get; set; } = [];
}

public class CreateThesisReviewCommentDTO
{
    public long? EventId { get; set; }
    public long? ParentCommentId { get; set; }
    public string Body { get; set; } = null!;
    public string? CommentType { get; set; }
    public string? VisibilityScope { get; set; }
}
