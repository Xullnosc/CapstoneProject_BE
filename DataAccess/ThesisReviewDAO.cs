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


    public async Task<ThesisReview> UpsertReviewerReviewAsync(string thesisId, int reviewerId, string decision, string? note, string? fileUrl)
    {
        var existing = await _context.ThesisReviews
            .FirstOrDefaultAsync(r => r.ThesisId == thesisId);

        if (existing == null)
        {
            // This should normally not happen if reviewers are assigned first, but handle it
            existing = new ThesisReview { ThesisId = thesisId };
            _context.ThesisReviews.Add(existing);
        }

        if (existing.Reviewer1Id == reviewerId)
        {
            existing.Reviewer1Decision = decision;
            existing.Reviewer1Comment = note;
            existing.Reviewer1FileUrl = fileUrl;
            existing.Reviewer1Date = DateTime.UtcNow;
        }
        else if (existing.Reviewer2Id == reviewerId)
        {
            existing.Reviewer2Decision = decision;
            existing.Reviewer2Comment = note;
            existing.Reviewer2FileUrl = fileUrl;
            existing.Reviewer2Date = DateTime.UtcNow;
        }
        else
        {
            // Fallback: If not assigned to a slot, assign to a free slot or throw
            if (existing.Reviewer1Id == null)
            {
                existing.Reviewer1Id = reviewerId;
                existing.Reviewer1Decision = decision;
                existing.Reviewer1Comment = note;
                existing.Reviewer1FileUrl = fileUrl;
                existing.Reviewer1Date = DateTime.UtcNow;
            }
            else if (existing.Reviewer2Id == null)
            {
                existing.Reviewer2Id = reviewerId;
                existing.Reviewer2Decision = decision;
                existing.Reviewer2Comment = note;
                existing.Reviewer2FileUrl = fileUrl;
                existing.Reviewer2Date = DateTime.UtcNow;
            }
            else
            {
                throw new InvalidOperationException("Both reviewer slots are already occupied by different users.");
            }
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<List<ThesisReview>> GetReviewsAsync(string thesisId)
    {
        var r = await _context.ThesisReviews.AsNoTracking().FirstOrDefaultAsync(x => x.ThesisId == thesisId);
        if (r == null) return new List<ThesisReview>();
        return new List<ThesisReview> { r }; // Note: The service might need adjustment if it expects multiple rows
    }

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
                Comment = note,
                DecidedAt = DateTime.UtcNow
            };
            _context.ThesisHodDecisions.Add(created);
            await _context.SaveChangesAsync();
            return created;
        }

        existing.HodId = hodId;
        existing.Decision = decision;
        existing.Comment = note;
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

        var review = await _context.ThesisReviews.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ThesisId == thesisId);

        var hod = await _context.ThesisHodDecisions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.ThesisId == thesisId);

        var reviewerIds = new List<int>();
        if (review?.Reviewer1Id != null) reviewerIds.Add(review.Reviewer1Id.Value);
        if (review?.Reviewer2Id != null) reviewerIds.Add(review.Reviewer2Id.Value);

        var reviewerUids = reviewerIds.Distinct().ToList();
        var relevantUserIds = new List<int>(reviewerUids);
        if (hod != null) relevantUserIds.Add(hod.HodId);

        var users = await _context.Users.AsNoTracking()
            .Where(u => relevantUserIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.Email, u.FullName })
            .ToListAsync();

        var status = new ThesisReviewStatusDTO
        {
            ThesisId = thesisId,
            ThesisStatus = thesis.Status,
            Reviewers = new List<ReviewerProgressDTO>()
        };

        if (review != null)
        {
            if (review.Reviewer1Id.HasValue)
            {
                var u1 = users.FirstOrDefault(x => x.UserId == review.Reviewer1Id.Value);
                status.Reviewers.Add(new ReviewerProgressDTO
                {
                    UserId = review.Reviewer1Id.Value,
                    Email = u1?.Email,
                    FullName = u1?.FullName,
                    Decision = review.Reviewer1Decision,
                    Comment = review.Reviewer1Comment,
                    FileUrl = review.Reviewer1FileUrl,
                    ReviewedAt = review.Reviewer1Date
                });
            }
            if (review.Reviewer2Id.HasValue)
            {
                var u2 = users.FirstOrDefault(x => x.UserId == review.Reviewer2Id.Value);
                status.Reviewers.Add(new ReviewerProgressDTO
                {
                    UserId = review.Reviewer2Id.Value,
                    Email = u2?.Email,
                    FullName = u2?.FullName,
                    Decision = review.Reviewer2Decision,
                    Comment = review.Reviewer2Comment,
                    FileUrl = review.Reviewer2FileUrl,
                    ReviewedAt = review.Reviewer2Date
                });
            }
        }

        // Compute overall status logic
        var decided = status.Reviewers.Where(x => !string.IsNullOrWhiteSpace(x.Decision)).ToList();
        
        if (reviewerIds.Count < 2)
        {
            status.OverallStatus = "Pending";
            status.RequiresHodDecision = false;
        }
        else if (decided.Count < 2)
        {
            status.OverallStatus = "Pending";
            status.RequiresHodDecision = false;
        }
        else
        {
            var passCount = decided.Count(x => string.Equals(x.Decision, "Pass", StringComparison.OrdinalIgnoreCase));
            if (passCount == 2)
            {
                status.OverallStatus = "Pass";
            }
            else if (passCount == 0)
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
                Comment = hod.Comment,
                DecidedAt = hod.DecidedAt
            };
            status.OverallStatus = "HodDecided";
            status.RequiresHodDecision = false;
        }

        return status;
    }

    public async Task InitializeReviewersAsync(string thesisId, int reviewer1Id, int reviewer2Id)
    {
        var existing = await _context.ThesisReviews.FirstOrDefaultAsync(r => r.ThesisId == thesisId);
        if (existing == null)
        {
            existing = new ThesisReview
            {
                ThesisId = thesisId,
                Reviewer1Id = reviewer1Id,
                Reviewer2Id = reviewer2Id
            };
            _context.ThesisReviews.Add(existing);
        }
        else
        {
            existing.Reviewer1Id = reviewer1Id;
            existing.Reviewer2Id = reviewer2Id;
            // Optionally reset decisions if HOD reassigns? Assuming reassign clears old decisions for slots.
            existing.Reviewer1Decision = null;
            existing.Reviewer2Decision = null;
            existing.Reviewer1Comment = null;
            existing.Reviewer2Comment = null;
            existing.Reviewer1FileUrl = null;
            existing.Reviewer2FileUrl = null;
            existing.Reviewer1Date = null;
            existing.Reviewer2Date = null;
        }

        await _context.SaveChangesAsync();
    }
}
