using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Models
{
    [Table("thesis_forms")]
    public class ThesisForm
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        public int VersionNumber { get; set; } = 1;

        [Required]
        public int UploadedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UploadedBy")]
        public virtual User? Uploader { get; set; }

        public virtual ICollection<ThesisFormHistory> Histories { get; set; } = new List<ThesisFormHistory>();
    }
}
