using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class CreateTeamDTO
    {
        [Required]
        [MinLength(3, ErrorMessage = "Team name must be at least 3 characters")]
        public string TeamName { get; set; }

        public string? Description { get; set; }
    }
}
