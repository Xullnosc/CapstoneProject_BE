using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ThesisReviewEvent
{
    public long Id { get; set; }

    public string ThesisId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public int ActorUserId { get; set; }

    public string ActorRole { get; set; } = null!;

    public string? Decision { get; set; }

    public string? PreviousDecision { get; set; }

    public int SequenceNo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Thesis Thesis { get; set; } = null!;

    public virtual User ActorUser { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<ThesisReviewComment> Comments { get; set; } =
        new List<ThesisReviewComment>();
}
