using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Models
{
    [Table("thesis_form_histories")]
    public class ThesisFormHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ThesisFormId { get; set; }

        [Required]
        [MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        public int UploadedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ThesisFormId")]
        public virtual ThesisForm? ThesisForm { get; set; }

        [ForeignKey("UploadedBy")]
        public virtual User? Uploader { get; set; }
    }
}
