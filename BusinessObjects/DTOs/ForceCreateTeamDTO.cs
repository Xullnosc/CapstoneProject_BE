using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class ForceCreateTeamDTO
    {
        [Required]
        [MinLength(3, ErrorMessage = "Team name must be at least 3 characters")]
        public string TeamName { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public int SemesterId { get; set; }

        [Required]
        public string LeaderEmail { get; set; } = null!;

        [Required]
        [MinLength(1, ErrorMessage = "At least one member is required")]
        public List<string> MemberEmails { get; set; } = new();
    }
}
