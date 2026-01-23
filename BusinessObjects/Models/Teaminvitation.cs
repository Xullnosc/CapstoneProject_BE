using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Teaminvitation
{
    public int InvitationId { get; set; }

    public int TeamId { get; set; }

    public int StudentId { get; set; }

    public int InvitedBy { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public virtual User InvitedByNavigation { get; set; } = null!;

    public virtual User Student { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
