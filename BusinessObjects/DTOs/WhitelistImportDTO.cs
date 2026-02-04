namespace BusinessObjects.DTOs
{
    public class WhitelistImportDTO
    {
        public string Email { get; set; } = null!;

        public string? StudentCode { get; set; }

        public string? FullName { get; set; }

        public int? RoleId { get; set; }

        public string? Campus { get; set; }

        public int? SemesterId { get; set; }
    }
}
