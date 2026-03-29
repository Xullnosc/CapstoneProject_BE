namespace Services.DTOs
{
    public class UserInfoDTO
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string? StudentCode { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? RoleName { get; set; }
        public string? Campus { get; set; }
        public int CampusId { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool HasTeam { get; set; }
        public int? PendingInvitationId { get; set; }
        public bool IsReviewer { get; set; }
        public int? LecturerId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? GithubLink { get; set; }
        public string? LinkedInLink { get; set; }
        public string? FacebookLink { get; set; }
        // Account detail (sinh viên)
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? Major { get; set; }
        public string? PersonalId { get; set; }
        public string? PlaceOfBirth { get; set; }
        public int? EnrollmentYear { get; set; }
    }
}
