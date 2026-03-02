namespace BusinessObjects.DTOs;

/// <summary>
/// Reviewer evaluation: set thesis status to Published (pass), Rejected (fail), or Need Update.
/// </summary>
public class ReviewThesisDTO
{
    public string Status { get; set; } = null!; // "Published" | "Rejected" | "Need Update"
    public string? Note { get; set; }
}
