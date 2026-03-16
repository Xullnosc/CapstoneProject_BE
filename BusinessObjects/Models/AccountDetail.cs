using System;

namespace BusinessObjects.Models;

public partial class AccountDetail
{
    public int AccountDetailId { get; set; }

    public int UserId { get; set; }

    public string? PhoneNumber { get; set; }

    public string? GithubLink { get; set; }

    public string? LinkedInLink { get; set; }

    public string? FacebookLink { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string? Major { get; set; }

    public string? PersonalId { get; set; }

    public string? PlaceOfBirth { get; set; }

    public int? EnrollmentYear { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
