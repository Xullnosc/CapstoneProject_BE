using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Models
{
    [Table("ImportBatches")]
    public class ImportBatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ImportBatchId { get; set; }

        [Required]
        [MaxLength(1024)]
        public string FileUrl { get; set; } = null!;

        [MaxLength(256)]
        public string? OriginalFileName { get; set; }

        [MaxLength(256)]
        public string? UploadedBy { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int? AffectedSemesterId { get; set; }

        [Required]
        public int Version { get; set; } = 1;

        [MaxLength(1024)]
        public string? Notes { get; set; }

        [ForeignKey("AffectedSemesterId")]
        public virtual Semester? AffectedSemester { get; set; }
    }
}
