using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string? StudentCode { get; set; }

    public string? FullName { get; set; }

    public string? Avatar { get; set; }

    public int? RoleId { get; set; }

    public bool? IsAuthorized { get; set; }

    public int? CampusId { get; set; }

    public virtual Campus CampusNavigation { get; set; } = null!;

    public DateTime? LastLogin { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Role? Role { get; set; }

    public virtual ICollection<Teaminvitation> TeaminvitationInvitedByNavigations { get; set; } = new List<Teaminvitation>();

    public virtual ICollection<Teaminvitation> TeaminvitationReceivers { get; set; } = new List<Teaminvitation>();

    public virtual ICollection<Teammember> Teammembers { get; set; } = new List<Teammember>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual ICollection<Thesis> Theses { get; set; } = new List<Thesis>();
    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();

    public virtual AccountDetail? AccountDetail { get; set; }
}
