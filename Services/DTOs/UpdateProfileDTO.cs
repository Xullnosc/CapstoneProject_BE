namespace Services.DTOs
{
    public class UpdateProfileDTO
    {
        public string? FullName { get; set; }
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
