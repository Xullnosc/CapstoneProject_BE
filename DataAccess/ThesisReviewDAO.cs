using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess;

public class ThesisReviewDAO : IThesisReviewDAO
{
    private readonly FctmsContext _context;

    public ThesisReviewDAO(FctmsContext context)
    {
        _context = context;
    }

    public async Task<List<ThesisReviewerAssignment>> ReplaceAssignmentsAsync(string thesisId, IEnumerable<int> reviewerIds, int? assignedBy)
    {
        // Reviewer assignment is derived from Team.MentorId/MentorId2 now.
        // Keep method for backward compatibility; no-op.
        return new List<ThesisReviewerAssignment>();
    }

    public Task<List<ThesisReviewerAssignment>> GetAssignmentsAsync(string thesisId)
        // Reviewer assignment is derived from Team.MentorId/MentorId2 now.
        // Keep method for backward compatibility; return empty.
        => Task.FromResult(new List<ThesisReviewerAssignment>());

    public async Task<ThesisReview> UpsertReviewerReviewAsync(string thesisId, int reviewerId, string decision, string? note)
    {
        var existing = await _context.ThesisReviews
            .FirstOrDefaultAsync(r => r.ThesisId == thesisId && r.ReviewerId == reviewerId);

        if (existing == null)
        {
            var created = new ThesisReview
            {
                ThesisId = thesisId,
                ReviewerId = reviewerId,
                Decision = decision,
                Note = note,
                ReviewedAt = DateTime.UtcNow
            };
            _context.ThesisReviews.Add(created);
            await _context.SaveChangesAsync();
            return created;
        }

        existing.Decision = decision;
        existing.Note = note;
        existing.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return existing;
    }

    public Task<List<ThesisReview>> GetReviewsAsync(string thesisId)
        => _context.ThesisReviews
            .AsNoTracking()
            .Where(r => r.ThesisId == thesisId)
            .OrderBy(r => r.Id)
            .ToListAsync();

    public async Task<ThesisHodDecision> UpsertHodDecisionAsync(string thesisId, int hodId, string decision, string? note)
    {
        var existing = await _context.ThesisHodDecisions
            .FirstOrDefaultAsync(d => d.ThesisId == thesisId);

        if (existing == null)
        {
            var created = new ThesisHodDecision
            {
                ThesisId = thesisId,
                HodId = hodId,
                Decision = decision,
                Note = note,
                DecidedAt = DateTime.UtcNow
            };
            _context.ThesisHodDecisions.Add(created);
            await _context.SaveChangesAsync();
            return created;
        }

        existing.HodId = hodId;
        existing.Decision = decision;
        existing.Note = note;
        existing.DecidedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return existing;
    }

    public Task<ThesisHodDecision?> GetHodDecisionAsync(string thesisId)
        => _context.ThesisHodDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ThesisId == thesisId);

    public async Task<ThesisReviewStatusDTO> GetReviewStatusAsync(string thesisId)
    {
        var thesis = await _context.Theses.AsNoTracking().FirstOrDefaultAsync(t => t.ThesisId == thesisId);
        if (thesis == null) throw new KeyNotFoundException("Thesis not found.");

        // Resolve 2 reviewers from Team.MentorId and Team.MentorId2 (mentors of thesis owner's team)
        var activeSemester = await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.Status == "Active");
        var now = DateTime.UtcNow;
        var currentSemester = activeSemester
            ?? await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now);

        if (currentSemester == null)
            throw new InvalidOperationException("Current semester not found.");

        var team = await _context.Teams
            .AsNoTracking()
            .Include(t => t.Teammembers)
            .Where(t => t.SemesterId == currentSemester.SemesterId && t.Status != "Disbanded")
            .FirstOrDefaultAsync(t => t.Teammembers.Any(tm => tm.StudentId == thesis.UserId));

        var reviewerIds = new List<int>();
        if (team?.MentorId != null) reviewerIds.Add(team.MentorId.Value);
        if (team?.MentorId2 != null) reviewerIds.Add(team.MentorId2.Value);
        reviewerIds = reviewerIds.Distinct().ToList();

        var reviews = await _context.ThesisReviews.AsNoTracking()
            .Where(r => r.ThesisId == thesisId)
            .ToListAsync();

        var hod = await _context.ThesisHodDecisions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.ThesisId == thesisId);

        var users = await _context.Users.AsNoTracking()
            .Where(u => reviewerIds.Contains(u.UserId) || (hod != null && u.UserId == hod.HodId))
            .Select(u => new { u.UserId, u.Email, u.FullName })
            .ToListAsync();

        var byReviewer = reviews.ToDictionary(r => r.ReviewerId, r => r);

        var status = new ThesisReviewStatusDTO
        {
            ThesisId = thesisId,
            ThesisStatus = thesis.Status,
            Reviewers = reviewerIds.Select(id =>
            {
                var u = users.FirstOrDefault(x => x.UserId == id);
                byReviewer.TryGetValue(id, out var r);
                return new ReviewerProgressDTO
                {
                    UserId = id,
                    Email = u?.Email,
                    FullName = u?.FullName,
                    Decision = r?.Decision,
                    Note = r?.Note,
                    ReviewedAt = r?.ReviewedAt
                };
            }).ToList()
        };

        // Compute overall status for UI
        var decided = status.Reviewers.Where(x => !string.IsNullOrWhiteSpace(x.Decision)).ToList();
        if (reviewerIds.Count == 0)
        {
            status.OverallStatus = "Pending";
            status.RequiresHodDecision = false;
        }
        else if (decided.Count < reviewerIds.Count)
        {
            status.OverallStatus = "Pending";
            status.RequiresHodDecision = false;
        }
        else
        {
            var passCount = decided.Count(x => string.Equals(x.Decision, "Pass", StringComparison.OrdinalIgnoreCase));
            var failCount = decided.Count - passCount;
            if (passCount == decided.Count)
            {
                status.OverallStatus = "Pass";
            }
            else if (failCount == decided.Count)
            {
                status.OverallStatus = "Fail";
            }
            else
            {
                status.OverallStatus = "Split";
                status.RequiresHodDecision = true;
            }
        }

        if (hod != null)
        {
            var u = users.FirstOrDefault(x => x.UserId == hod.HodId);
            status.HodDecision = new HodDecisionDTO
            {
                HodId = hod.HodId,
                Email = u?.Email,
                FullName = u?.FullName,
                Decision = hod.Decision,
                Note = hod.Note,
                DecidedAt = hod.DecidedAt
            };
            status.OverallStatus = "HodDecided";
            status.RequiresHodDecision = false;
        }

        return status;
    }
}

