using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Thesis
{
    public string ThesisId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? ShortDescription { get; set; }

    public int UserId { get; set; }

    public int? SemesterId { get; set; }

    public DateTime? UpDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string? FileUrl { get; set; }

    public string? Status { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Semester? Semester { get; set; }

    public virtual ICollection<ThesisHistory> ThesisHistories { get; set; } = new List<ThesisHistory>();
}
