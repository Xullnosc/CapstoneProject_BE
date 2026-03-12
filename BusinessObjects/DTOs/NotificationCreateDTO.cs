using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class NotificationCreateDTO
    {
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        public bool SendEmail { get; set; } = true;
    }
}