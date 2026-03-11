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
}

