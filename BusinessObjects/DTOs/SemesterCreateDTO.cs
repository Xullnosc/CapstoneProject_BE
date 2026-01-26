using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class SemesterCreateDTO
    {
        public int SemesterId { get; set; }

        [Required]
        public string SemesterName { get; set; } = null!;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool? IsActive { get; set; }
    }
}
