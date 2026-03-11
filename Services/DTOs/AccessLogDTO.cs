using System;

namespace Services.DTOs
{
    public class AccessLogDTO
    {
        public string Id { get; set; } = null!;
        public int? UserId { get; set; }
        public string UserEmail { get; set; } = null!;
        public string? IpAddress { get; set; }
        public string Action { get; set; } = null!;
        public bool IsSuccess { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Include basic user info if needed
        public string? FullName { get; set; }
        public string? RoleName { get; set; }
    }
}
