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
        public DateTime? LastLogin { get; set; }
    }
}
