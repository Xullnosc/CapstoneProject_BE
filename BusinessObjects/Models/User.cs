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

    public string? Campus { get; set; }

    public DateTime? LastLogin { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<Team> TeamLeaders { get; set; } = new List<Team>();

    public virtual ICollection<Team> TeamMentorId2Navigations { get; set; } = new List<Team>();

    public virtual ICollection<Team> TeamMentors { get; set; } = new List<Team>();

    public virtual ICollection<Teaminvitation> TeaminvitationInvitedByNavigations { get; set; } = new List<Teaminvitation>();

    public virtual ICollection<Teaminvitation> TeaminvitationStudents { get; set; } = new List<Teaminvitation>();

    public virtual ICollection<Teammember> Teammembers { get; set; } = new List<Teammember>();

    public virtual ICollection<Thesis> Theses { get; set; } = new List<Thesis>();

    public virtual ICollection<ThesisFormHistory> ThesisFormHistories { get; set; } = new List<ThesisFormHistory>();

    public virtual ICollection<ThesisForm> ThesisForms { get; set; } = new List<ThesisForm>();

    public virtual ICollection<ThesisHistory> ThesisHistories { get; set; } = new List<ThesisHistory>();
}
