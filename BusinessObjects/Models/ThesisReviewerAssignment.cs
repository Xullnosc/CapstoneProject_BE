using System;

namespace BusinessObjects.Models;

public partial class ThesisReviewerAssignment
{
    public long Id { get; set; }
    public string ThesisId { get; set; } = null!;
    public int ReviewerId { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
}

