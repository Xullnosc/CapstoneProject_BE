using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs;

public class ThesisReviewStatusDTO
{
    public string ThesisId { get; set; } = null!;
    public string? ThesisStatus { get; set; }

    /// <summary>
    /// "Pending" | "Pass" | "Fail" | "Split" | "HodDecided"
    /// </summary>
    public string OverallStatus { get; set; } = "Pending";

    public bool RequiresHodDecision { get; set; }

    public List<ReviewerProgressDTO> Reviewers { get; set; } = [];

    public HodDecisionDTO? HodDecision { get; set; }
}

public class ReviewerProgressDTO
{
    public int UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Avatar { get; set; }
    public string? Decision { get; set; } // Pass | Fail
    public string? Comment { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class HodDecisionDTO
{
    public int HodId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Avatar { get; set; }
    public string Decision { get; set; } = null!;
    public string? Comment { get; set; }
    public DateTime DecidedAt { get; set; }
}

