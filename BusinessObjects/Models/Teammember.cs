using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Teammember
{
    public int TeamMemberId { get; set; }

    public int TeamId { get; set; }

    public int StudentId { get; set; }

    public string? Role { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual User Student { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
