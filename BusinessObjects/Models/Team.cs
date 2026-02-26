using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Team
{
    public int TeamId { get; set; }

    public string TeamCode { get; set; } = null!;

    public string TeamName { get; set; } = null!;

    public string? TeamAvatar { get; set; }

    public string? Description { get; set; }

    public int SemesterId { get; set; }

    public int LeaderId { get; set; }

    public int? MentorId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Leader { get; set; } = null!;

    public virtual Semester Semester { get; set; } = null!;

    public virtual User? Mentor { get; set; }

    public virtual ICollection<Teaminvitation> Teaminvitations { get; set; } = new List<Teaminvitation>();

    public virtual ICollection<Teammember> Teammembers { get; set; } = new List<Teammember>();
}
