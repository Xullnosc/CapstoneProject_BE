using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ThesisReview
{
    public int ReviewId { get; set; }

    public string ThesisId { get; set; } = null!;

    public int ReviewerId { get; set; }

    public string Status { get; set; } = null!;

    public string? Comment { get; set; }

    public string? FileUrl { get; set; }

    public DateTime? ReviewDate { get; set; }

    public virtual Thesis Thesis { get; set; } = null!;

    public virtual Lecturer Reviewer { get; set; } = null!;
}
