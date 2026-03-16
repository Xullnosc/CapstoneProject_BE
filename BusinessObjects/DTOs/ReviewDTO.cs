using System;

namespace BusinessObjects.DTOs;

public class ReviewDTO
{
    public long Id { get; set; }
    public string ThesisId { get; set; } = null!;
    public int? ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public string Decision { get; set; } = null!; // Pass | Fail
    public string? Comment { get; set; }
    public string? FileUrl { get; set; }
    public DateTime ReviewedAt { get; set; }
}
