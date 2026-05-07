using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    /// <summary>
    /// Aggregated statistics for the Admin dashboard.
    /// Covers all campuses (Admin JWT has no campus_id claim, so EF query filters are bypassed).
    /// </summary>
    public class AdminDashboardStatsDTO
    {
        /// <summary>Top-level totals (users, theses, teams, semesters).</summary>
        public DashboardStatsDTO Summary { get; set; } = new();

        /// <summary>User count grouped by role (e.g. Student, Lecturer, HOD, Admin).</summary>
        public List<RoleDistributionDTO> UsersByRole { get; set; } = new();

        /// <summary>Thesis count grouped by workflow status (Draft, Reviewing, Published, etc.).</summary>
        public List<ThesisStatusCountDTO> ThesesByStatus { get; set; } = new();

        /// <summary>Team and thesis counts per semester (last 8 semesters, chronological).</summary>
        public List<SemesterTeamCountDTO> TeamsBySemester { get; set; } = new();

        /// <summary>Successful login count per month (last 6 months). Format: "YYYY-MM".</summary>
        public List<MonthlyActivityDTO> MonthlyLogins { get; set; } = new();

        /// <summary>Team count grouped by status (Pending, Qualified, Insufficient, Disbanded).</summary>
        public List<TeamStatusCountDTO> TeamsByStatus { get; set; } = new();
    }

    /// <summary>User count for a single role.</summary>
    public class RoleDistributionDTO
    {
        public string Role { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>Team and thesis counts for a single semester, used in the semester comparison bar chart.</summary>
    public class SemesterTeamCountDTO
    {
        public string SemesterCode { get; set; } = string.Empty;
        public int TeamCount { get; set; }
        public int ThesisCount { get; set; }
    }

    /// <summary>Login count for a single month, used in the login activity line chart.</summary>
    public class MonthlyActivityDTO
    {
        /// <summary>Month in "YYYY-MM" format (e.g. "2026-05").</summary>
        public string Month { get; set; } = string.Empty;
        public int LoginCount { get; set; }
    }

    /// <summary>Team count for a single status value.</summary>
    public class TeamStatusCountDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
