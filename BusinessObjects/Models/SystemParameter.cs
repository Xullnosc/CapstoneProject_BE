using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Models
{
    public class SystemParameter
    {
        [Key]
        [MaxLength(255)]
        public string Key { get; set; } = null!;

        [Required]
        public string Value { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
