using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ThesisReviewComment
{
    public long Id { get; set; }

    public long EventId { get; set; }

    public string ThesisId { get; set; } = null!;

    public long? ParentCommentId { get; set; }

    public int AuthorUserId { get; set; }

    public string Body { get; set; } = null!;

    public string CommentType { get; set; } = null!;

    public string VisibilityScope { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ThesisReviewEvent Event { get; set; } = null!;

    public virtual Thesis Thesis { get; set; } = null!;

    public virtual ThesisReviewComment? ParentComment { get; set; }

    public virtual ICollection<ThesisReviewComment> Replies { get; set; } =
        new List<ThesisReviewComment>();

    public virtual User AuthorUser { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<ThesisReviewAttachment> Attachments { get; set; } =
        new List<ThesisReviewAttachment>();
}
