using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;
public enum NotificationType
{
    TeamInvitation,
    ThesisUpdate,
    MentorChange,
    SemesterDeadline,
    ChecklistUpdate,
    HODAction,
    SystemAnnouncement,
    FormSubmission
}
public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    /// <summary>
    /// Team, Thesis, Checklist, etc.
    /// </summary>
    public string? RelatedEntityType { get; set; }

    public int? RelatedEntityId { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
