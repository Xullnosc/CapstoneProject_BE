using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Models
{
    [Table("access_logs")]
    public class AccessLog
    {
        [Key]
        [Column("Id")]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("UserId")]
        public int? UserId { get; set; }

        [Required]
        [Column("UserEmail")]
        [StringLength(255)]
        public string UserEmail { get; set; } = null!;

        [Column("IpAddress")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Required]
        [Column("Action")]
        [StringLength(100)]
        public string Action { get; set; } = null!;

        [Required]
        [Column("IsSuccess")]
        public bool IsSuccess { get; set; } = true;

        [Column("Description")]
        public string? Description { get; set; }

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
