using System.ComponentModel.DataAnnotations;

namespace CapstoneProject_BE.DTOs.Requests
{
    public class WhitelistUpsertRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? StudentCode { get; set; }

        public string? FullName { get; set; }

        [Range(1, int.MaxValue)]
        public int RoleId { get; set; }

        public string? Avatar { get; set; }

        public string? Campus { get; set; }

        public int? CampusId { get; set; }

        [Range(1, int.MaxValue)]
        public int SemesterId { get; set; }
    }
}
