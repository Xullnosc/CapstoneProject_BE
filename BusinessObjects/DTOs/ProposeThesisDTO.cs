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

        [Required(ErrorMessage = "English name is required")]
        public string ThesisNameEn { get; set; } = null!;

        [Required(ErrorMessage = "Vietnamese name is required")]
        public string ThesisNameVi { get; set; } = null!;

        [Required(ErrorMessage = "Abbreviation is required")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Abbreviation must only contain English letters and numbers")]
        [StringLength(5, ErrorMessage = "Abbreviation cannot exceed 5 characters")]
        public string Abbreviation { get; set; } = null!;

        public bool IsFromEnterprise { get; set; }

        public string? EnterpriseName { get; set; }

        public bool IsApplied { get; set; }

        public bool IsAppUsed { get; set; }
        
        public int? AuthorId { get; set; }
    }
}
