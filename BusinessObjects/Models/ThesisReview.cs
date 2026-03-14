using System;

namespace BusinessObjects.Models;

public partial class ThesisReview
{
    public string ThesisId { get; set; } = null!;

    public int? Reviewer1Id { get; set; }
    public int? Reviewer2Id { get; set; }

    public string? Reviewer1Decision { get; set; }
    public string? Reviewer2Decision { get; set; }

    public string? Reviewer1Comment { get; set; }
    public string? Reviewer2Comment { get; set; }

    public string? Reviewer1FileUrl { get; set; }
    public string? Reviewer2FileUrl { get; set; }

    public DateTime? Reviewer1Date { get; set; }
    public DateTime? Reviewer2Date { get; set; }

    public virtual Thesis Thesis { get; set; } = null!;
    public virtual Lecturer? Reviewer1 { get; set; }
    public virtual Lecturer? Reviewer2 { get; set; }
}
