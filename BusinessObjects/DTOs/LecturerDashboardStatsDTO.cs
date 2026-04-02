using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs;

public class LecturerDashboardStatsDTO
{
    public string? CurrentSemesterCode { get; set; }

    public LecturerMentorStatsDTO Mentor { get; set; } = new();

    public LecturerApplicationStatsDTO Applications { get; set; } = new();

    public LecturerOwnThesisStatsDTO OwnTheses { get; set; } = new();

    /// <summary>Populated when the user is a reviewer (claim IsReviewer).</summary>
    public LecturerReviewerStatsDTO? Reviewer { get; set; }

    public int UnreadNotifications { get; set; }

    public DashboardStatsDTO CampusSummary { get; set; } = new();
}

public class LecturerMentorStatsDTO
{
    public int MentoredTeamsInCurrentSemester { get; set; }

    public int MaxMentorTeamsPerSemester { get; set; } = 4;

    public int InvitationsPending { get; set; }

    public int InvitationsAccepted { get; set; }

    public int InvitationsDeclined { get; set; }

    public int InvitationsCancelled { get; set; }

    public List<RecentMentorInvitationRowDTO> RecentInvitations { get; set; } = new();
}

public class RecentMentorInvitationRowDTO
{
    public int InvitationId { get; set; }

    public int TeamId { get; set; }

    public string TeamCode { get; set; } = "";

    public string TeamName { get; set; } = "";

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public class LecturerApplicationStatsDTO
{
    public int PendingCount { get; set; }

    public int ApprovedCount { get; set; }

    public int RejectedCount { get; set; }

    public int CancelledCount { get; set; }

    public List<RecentApplicationRowDTO> RecentPending { get; set; } = new();
}

public class RecentApplicationRowDTO
{
    public int ApplicationId { get; set; }

    public string ThesisId { get; set; } = "";

    public string ThesisTitle { get; set; } = "";

    public int TeamId { get; set; }

    public string TeamCode { get; set; } = "";

    public string TeamName { get; set; } = "";

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public class LecturerOwnThesisStatsDTO
{
    public int TotalInCurrentSemester { get; set; }

    public List<ThesisStatusCountDTO> ByStatus { get; set; } = new();
}

public class ThesisStatusCountDTO
{
    public string Status { get; set; } = "";

    public int Count { get; set; }
}

public class LecturerReviewerStatsDTO
{
    public int PendingReviewCount { get; set; }

    public List<ReviewerPendingThesisRowDTO> PendingTheses { get; set; } = new();
}

public class ReviewerPendingThesisRowDTO
{
    public string ThesisId { get; set; } = "";

    public string Title { get; set; } = "";

    public string? ThesisStatus { get; set; }
}
