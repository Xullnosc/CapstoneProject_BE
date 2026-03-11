using System;

namespace BusinessObjects.DTOs;

public class ReviewDTO
{
    public int ReviewId { get; set; }
    public string ThesisId { get; set; } = null!;
    public int ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public string Status { get; set; } = null!;
    public string? Comment { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? ReviewDate { get; set; }
}
