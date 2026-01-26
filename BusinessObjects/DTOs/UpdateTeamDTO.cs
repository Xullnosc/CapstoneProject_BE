using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BusinessObjects.DTOs
{
    public class UpdateTeamDTO
    {
        [MinLength(3, ErrorMessage = "Team name must be at least 3 characters")]
        public string TeamName { get; set; }

        public string Description { get; set; }

        public string? TeamAvatar { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }
}
