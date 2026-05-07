using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class DashboardDAO : IDashboardDAO
    {
        private readonly FctmsContext _context;

        public DashboardDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDTO> GetDashboardStatsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalTheses = await _context.Theses.CountAsync();
            var totalTeams = await _context.Teams.CountAsync();
            var totalSemesters = await _context.Semesters.CountAsync();

            return new DashboardStatsDTO
            {
                TotalUsers = totalUsers,
                TotalTheses = totalTheses,
                TotalTeams = totalTeams,
                TotalSemesters = totalSemesters
            };
        }

        public async Task<LecturerDashboardStatsDTO> GetLecturerDashboardStatsAsync(
            int userId,
            bool includeReviewerSection
        )
        {
            var semester = await ResolveCurrentSemesterAsync();
            int? semesterId = semester?.SemesterId;

            var campusSummary = await GetDashboardStatsAsync();

            var unread = await _context
                .Notifications.AsNoTracking()
                .CountAsync(n => n.UserId == userId && (n.IsRead != true));

            var mentorType = CampusConstants.InvitationType.Mentor;

            var mentorStats = new LecturerMentorStatsDTO();

            if (semesterId.HasValue)
            {
                mentorStats.MentoredTeamsInCurrentSemester = await _context
                    .Teams.AsNoTracking()
                    .CountAsync(t =>
                        t.SemesterId == semesterId.Value
                        && t.Status != CampusConstants.TeamStatus.Disbanded
                        && (t.MentorId == userId || t.MentorId2 == userId)
                    );

                mentorStats.InvitationsPending = await _context
                    .Teaminvitations.AsNoTracking()
                    .Where(i =>
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && i.Status == CampusConstants.InvitationStatus.Pending
                    )
                    .CountAsync(i =>
                        _context.Teams.Any(t =>
                            t.TeamId == i.TeamId && t.SemesterId == semesterId.Value
                        )
                    );

                mentorStats.InvitationsAccepted = await _context
                    .Teaminvitations.AsNoTracking()
                    .Where(i =>
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && i.Status == CampusConstants.InvitationStatus.Accepted
                    )
                    .CountAsync(i =>
                        _context.Teams.Any(t =>
                            t.TeamId == i.TeamId && t.SemesterId == semesterId.Value
                        )
                    );

                mentorStats.InvitationsDeclined = await _context
                    .Teaminvitations.AsNoTracking()
                    .Where(i =>
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && i.Status == CampusConstants.InvitationStatus.Declined
                    )
                    .CountAsync(i =>
                        _context.Teams.Any(t =>
                            t.TeamId == i.TeamId && t.SemesterId == semesterId.Value
                        )
                    );

                mentorStats.InvitationsCancelled = await _context
                    .Teaminvitations.AsNoTracking()
                    .Where(i =>
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && i.Status == CampusConstants.InvitationStatus.Cancelled
                    )
                    .CountAsync(i =>
                        _context.Teams.Any(t =>
                            t.TeamId == i.TeamId && t.SemesterId == semesterId.Value
                        )
                    );

                mentorStats.RecentInvitations = await (
                    from i in _context.Teaminvitations.AsNoTracking()
                    join t in _context.Teams.AsNoTracking() on i.TeamId equals t.TeamId
                    where
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && t.SemesterId == semesterId.Value
                    orderby i.CreatedAt descending
                    select new RecentMentorInvitationRowDTO
                    {
                        InvitationId = i.InvitationId,
                        TeamId = t.TeamId,
                        TeamCode = t.TeamCode,
                        TeamName = t.TeamName,
                        Status = i.Status,
                        CreatedAt = i.CreatedAt
                    }
                ).Take(5).ToListAsync();
            }
            else
            {
                mentorStats.InvitationsPending = await _context
                    .Teaminvitations.AsNoTracking()
                    .CountAsync(i =>
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && i.Status == CampusConstants.InvitationStatus.Pending
                    );

                mentorStats.InvitationsAccepted = await _context
                    .Teaminvitations.AsNoTracking()
                    .CountAsync(i =>
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && i.Status == CampusConstants.InvitationStatus.Accepted
                    );

                mentorStats.InvitationsDeclined = await _context
                    .Teaminvitations.AsNoTracking()
                    .CountAsync(i =>
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && i.Status == CampusConstants.InvitationStatus.Declined
                    );

                mentorStats.InvitationsCancelled = await _context
                    .Teaminvitations.AsNoTracking()
                    .CountAsync(i =>
                        i.Type == mentorType
                        && i.ReceiverId == userId
                        && i.Status == CampusConstants.InvitationStatus.Cancelled
                    );

                mentorStats.RecentInvitations = await (
                    from i in _context.Teaminvitations.AsNoTracking()
                    join t in _context.Teams.AsNoTracking() on i.TeamId equals t.TeamId
                    where i.Type == mentorType && i.ReceiverId == userId
                    orderby i.CreatedAt descending
                    select new RecentMentorInvitationRowDTO
                    {
                        InvitationId = i.InvitationId,
                        TeamId = t.TeamId,
                        TeamCode = t.TeamCode,
                        TeamName = t.TeamName,
                        Status = i.Status,
                        CreatedAt = i.CreatedAt
                    }
                ).Take(5).ToListAsync();
            }

            var appStats = new LecturerApplicationStatsDTO();

            if (semesterId.HasValue)
            {
                appStats.PendingCount = await _context
                    .ThesisApplications.AsNoTracking()
                    .CountAsync(a =>
                        a.Status == "Pending"
                        && _context.Theses.Any(th =>
                            th.ThesisId == a.ThesisId
                            && th.UserId == userId
                            && th.SemesterId == semesterId.Value
                        )
                    );

                appStats.ApprovedCount = await _context
                    .ThesisApplications.AsNoTracking()
                    .CountAsync(a =>
                        a.Status == "Approved"
                        && _context.Theses.Any(th =>
                            th.ThesisId == a.ThesisId
                            && th.UserId == userId
                            && th.SemesterId == semesterId.Value
                        )
                    );

                appStats.RejectedCount = await _context
                    .ThesisApplications.AsNoTracking()
                    .CountAsync(a =>
                        a.Status == "Rejected"
                        && _context.Theses.Any(th =>
                            th.ThesisId == a.ThesisId
                            && th.UserId == userId
                            && th.SemesterId == semesterId.Value
                        )
                    );

                appStats.CancelledCount = await _context
                    .ThesisApplications.AsNoTracking()
                    .CountAsync(a =>
                        a.Status == "Cancelled"
                        && _context.Theses.Any(th =>
                            th.ThesisId == a.ThesisId
                            && th.UserId == userId
                            && th.SemesterId == semesterId.Value
                        )
                    );

                appStats.RecentPending = await (
                    from a in _context.ThesisApplications.AsNoTracking()
                    join th in _context.Theses.AsNoTracking() on a.ThesisId equals th.ThesisId
                    join tm in _context.Teams.AsNoTracking() on a.TeamId equals tm.TeamId
                    where
                        th.UserId == userId
                        && th.SemesterId == semesterId.Value
                        && a.Status == "Pending"
                    orderby a.CreatedAt descending
                    select new RecentApplicationRowDTO
                    {
                        ApplicationId = a.Id,
                        ThesisId = th.ThesisId,
                        ThesisTitle = th.Title,
                        TeamId = tm.TeamId,
                        TeamCode = tm.TeamCode,
                        TeamName = tm.TeamName,
                        Status = a.Status,
                        CreatedAt = a.CreatedAt
                    }
                ).Take(5).ToListAsync();
            }
            else
            {
                appStats.PendingCount = await _context
                    .ThesisApplications.AsNoTracking()
                    .CountAsync(a =>
                        a.Status == "Pending"
                        && _context.Theses.Any(th =>
                            th.ThesisId == a.ThesisId && th.UserId == userId
                        )
                    );

                appStats.ApprovedCount = await _context
                    .ThesisApplications.AsNoTracking()
                    .CountAsync(a =>
                        a.Status == "Approved"
                        && _context.Theses.Any(th =>
                            th.ThesisId == a.ThesisId && th.UserId == userId
                        )
                    );

                appStats.RejectedCount = await _context
                    .ThesisApplications.AsNoTracking()
                    .CountAsync(a =>
                        a.Status == "Rejected"
                        && _context.Theses.Any(th =>
                            th.ThesisId == a.ThesisId && th.UserId == userId
                        )
                    );

                appStats.CancelledCount = await _context
                    .ThesisApplications.AsNoTracking()
                    .CountAsync(a =>
                        a.Status == "Cancelled"
                        && _context.Theses.Any(th =>
                            th.ThesisId == a.ThesisId && th.UserId == userId
                        )
                    );

                appStats.RecentPending = await (
                    from a in _context.ThesisApplications.AsNoTracking()
                    join th in _context.Theses.AsNoTracking() on a.ThesisId equals th.ThesisId
                    join tm in _context.Teams.AsNoTracking() on a.TeamId equals tm.TeamId
                    where th.UserId == userId && a.Status == "Pending"
                    orderby a.CreatedAt descending
                    select new RecentApplicationRowDTO
                    {
                        ApplicationId = a.Id,
                        ThesisId = th.ThesisId,
                        ThesisTitle = th.Title,
                        TeamId = tm.TeamId,
                        TeamCode = tm.TeamCode,
                        TeamName = tm.TeamName,
                        Status = a.Status,
                        CreatedAt = a.CreatedAt
                    }
                ).Take(5).ToListAsync();
            }

            var ownTheses = new LecturerOwnThesisStatsDTO();
            if (semesterId.HasValue)
            {
                var ownQuery = _context.Theses.AsNoTracking().Where(t =>
                    t.UserId == userId && t.SemesterId == semesterId.Value
                );

                ownTheses.TotalInCurrentSemester = await ownQuery.CountAsync();

                ownTheses.ByStatus = await ownQuery
                    .GroupBy(t => t.Status ?? "")
                    .Select(g => new ThesisStatusCountDTO { Status = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();
            }
            else
            {
                var ownQuery = _context.Theses.AsNoTracking().Where(t => t.UserId == userId);
                ownTheses.TotalInCurrentSemester = await ownQuery.CountAsync();
                ownTheses.ByStatus = await ownQuery
                    .GroupBy(t => t.Status ?? "")
                    .Select(g => new ThesisStatusCountDTO { Status = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();
            }

            LecturerReviewerStatsDTO? reviewer = null;
            if (includeReviewerSection)
            {
                var assignedIds = await _context
                    .ThesisReviewEvents.AsNoTracking()
                    .Where(e =>
                        !e.IsDeleted
                        && e.EventType == "REVIEWER_ASSIGNED"
                        && e.ActorUserId == userId
                    )
                    .Select(e => e.ThesisId)
                    .Distinct()
                    .ToListAsync();

                var decidedIds = await _context
                    .ThesisReviewEvents.AsNoTracking()
                    .Where(e =>
                        !e.IsDeleted
                        && e.EventType == "REVIEWER_DECISION"
                        && e.ActorUserId == userId
                        && e.Decision != null
                        && e.Decision != ""
                    )
                    .Select(e => e.ThesisId)
                    .Distinct()
                    .ToListAsync();

                var pendingReviewIds = assignedIds.Except(decidedIds).ToList();

                reviewer = new LecturerReviewerStatsDTO
                {
                    PendingReviewCount = pendingReviewIds.Count,
                    PendingTheses = await _context
                        .Theses.AsNoTracking()
                        .Where(t => pendingReviewIds.Contains(t.ThesisId))
                        .OrderBy(t => t.Title)
                        .Take(5)
                        .Select(t => new ReviewerPendingThesisRowDTO
                        {
                            ThesisId = t.ThesisId,
                            Title = t.Title,
                            ThesisStatus = t.Status
                        })
                        .ToListAsync()
                };
            }

            return new LecturerDashboardStatsDTO
            {
                CurrentSemesterCode = semester?.SemesterCode,
                Mentor = mentorStats,
                Applications = appStats,
                OwnTheses = ownTheses,
                Reviewer = reviewer,
                UnreadNotifications = unread,
                CampusSummary = campusSummary
            };
        }

        /// <summary>
        /// Builds the full Admin dashboard payload in a single call.
        /// Admin tokens have no campus_id claim, so EF global query filters let all campus data through.
        /// </summary>
        public async Task<AdminDashboardStatsDTO> GetAdminDashboardStatsAsync()
        {
            var summary = await GetDashboardStatsAsync();

            // ── Users grouped by role (for pie/doughnut chart) ──
            var usersByRole = await (
                from u in _context.Users.AsNoTracking()
                join r in _context.Roles.AsNoTracking() on u.RoleId equals r.RoleId
                group u by r.RoleName into g
                select new RoleDistributionDTO
                {
                    Role = g.Key ?? "Unknown",
                    Count = g.Count()
                }
            ).OrderByDescending(x => x.Count).ToListAsync();

            // ── Theses grouped by workflow status (for doughnut chart + progress bars) ──
            var thesesByStatus = await _context.Theses.AsNoTracking()
                .GroupBy(t => t.Status ?? "Unknown")
                .Select(g => new ThesisStatusCountDTO { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // ── Teams & theses per semester (last 8, for grouped bar chart) ──
            // Fetched newest-first via Take(8), then reversed to chronological order for the chart x-axis.
            var teamsBySemester = await (
                from s in _context.Semesters.AsNoTracking()
                orderby s.StartDate descending
                select new SemesterTeamCountDTO
                {
                    SemesterCode = s.SemesterCode,
                    TeamCount = _context.Teams.Count(t => t.SemesterId == s.SemesterId),
                    ThesisCount = _context.Theses.Count(t => t.SemesterId == s.SemesterId)
                }
            ).Take(8).ToListAsync();
            teamsBySemester.Reverse();

            // ── Monthly login activity (last 6 months, for line/area chart) ──
            // Step 1: GROUP BY on the DB side with raw year/month integers.
            // Step 2: Format "YYYY-MM" string in C# — avoids MySQL syntax issues
            //         with string concatenation that Pomelo/EF Core can't translate.
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var rawLogins = await _context.AccessLogs.AsNoTracking()
                .Where(a => a.Action.StartsWith("Login") && a.IsSuccess && a.CreatedAt >= sixMonthsAgo)
                .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var monthlyLogins = rawLogins
                .Select(g => new MonthlyActivityDTO
                {
                    Month = $"{g.Year}-{g.Month:D2}",
                    LoginCount = g.Count
                })
                .OrderBy(x => x.Month)
                .ToList();

            // ── Teams grouped by status (for horizontal bar chart) ──
            var teamsByStatus = await _context.Teams.AsNoTracking()
                .GroupBy(t => t.Status ?? "Unknown")
                .Select(g => new TeamStatusCountDTO { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return new AdminDashboardStatsDTO
            {
                Summary = summary,
                UsersByRole = usersByRole,
                ThesesByStatus = thesesByStatus,
                TeamsBySemester = teamsBySemester,
                MonthlyLogins = monthlyLogins,
                TeamsByStatus = teamsByStatus
            };
        }

        public async Task<HodDashboardStatsDTO> GetHodDashboardStatsAsync()
        {
            var summary = await GetDashboardStatsAsync();
            var semester = await ResolveCurrentSemesterAsync();

            var thesesByStatus = await _context.Theses.AsNoTracking()
                .GroupBy(t => t.Status ?? "Unknown")
                .Select(g => new ThesisStatusCountDTO { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var teamsBySemester = await (
                from s in _context.Semesters.AsNoTracking()
                orderby s.StartDate descending
                select new SemesterTeamCountDTO
                {
                    SemesterCode = s.SemesterCode,
                    TeamCount = _context.Teams.Count(t => t.SemesterId == s.SemesterId),
                    ThesisCount = _context.Theses.Count(t => t.SemesterId == s.SemesterId)
                }
            ).Take(8).ToListAsync();
            teamsBySemester.Reverse();

            var teamsByStatus = await _context.Teams.AsNoTracking()
                .GroupBy(t => t.Status ?? "Unknown")
                .Select(g => new TeamStatusCountDTO { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return new HodDashboardStatsDTO
            {
                Summary = summary,
                CurrentSemesterCode = semester?.SemesterCode,
                ThesesByStatus = thesesByStatus,
                TeamsBySemester = teamsBySemester,
                TeamsByStatus = teamsByStatus
            };
        }

        private async Task<Semester?> ResolveCurrentSemesterAsync()
        {
            var activeSemester = await _context
                .Semesters.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Status == CampusConstants.SemesterStatus.Open);

            if (activeSemester != null)
            {
                return activeSemester;
            }

            var now = DateTime.UtcNow;
            return await _context
                .Semesters.AsNoTracking()
                .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now);
        }
    }
}

