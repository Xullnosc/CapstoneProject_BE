using System;

namespace BusinessObjects.Models;

public partial class ThesisReview
{
    public long Id { get; set; }
    public string ThesisId { get; set; } = null!;
    public int ReviewerId { get; set; }
    public string Decision { get; set; } = null!; // Pass | Fail
    public string? Note { get; set; }
    public DateTime ReviewedAt { get; set; }

    // Backward compatibility aliases for older workflow code.
    public int ReviewId
    {
        get => (int)Id;
        set => Id = value;
    }

    public string Status
    {
        get => Decision;
        set => Decision = value;
    }

    public string? Comment
    {
        get => Note;
        set => Note = value;
    }

    public DateTime? ReviewDate
    {
        get => ReviewedAt;
        set => ReviewedAt = value ?? ReviewedAt;
    }

    public string? FileUrl { get; set; }

    public virtual Thesis Thesis { get; set; } = null!;

    public virtual Lecturer Reviewer { get; set; } = null!;
}
