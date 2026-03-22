using System;

namespace BusinessObjects.Models;

public partial class ThesisReviewAttachment
{
    public long Id { get; set; }

    public long CommentId { get; set; }

    public string ThesisId { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string? MimeType { get; set; }

    public long? FileSize { get; set; }

    public int UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ThesisReviewComment Comment { get; set; } = null!;

    public virtual Thesis Thesis { get; set; } = null!;

    public virtual User UploadedByNavigation { get; set; } = null!;
}
