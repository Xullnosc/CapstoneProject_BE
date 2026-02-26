using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class ProposeThesisDTO
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = null!;

        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Proposal document is required")]
        public IFormFile File { get; set; } = null!;
    }
}
