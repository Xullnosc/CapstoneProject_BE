using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class SemesterCreateDTO
    {
        public int SemesterId { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "SemesterCode must be exactly 4 characters.")]
        [RegularExpression(@"^(SP|SU|FA)\d{2}$", ErrorMessage = "SemesterCode must be in format SPxx, SUxx, FAxx (e.g., SP26)")]
        public string SemesterCode { get; set; } = null!;

        [Required]
        public string SemesterName { get; set; } = null!;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool? IsActive { get; set; }
    }
}
