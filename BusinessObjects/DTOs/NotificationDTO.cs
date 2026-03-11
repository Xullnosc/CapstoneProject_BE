using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class NotificationDTO
    {
        public int NotificationId { get; set; }

        public int UserId { get; set; }

        [Required]
        public string Type { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        public bool IsRead { get; set; }

        public DateTime? ReadAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateNotificationDTO
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Type { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }
    }
}
