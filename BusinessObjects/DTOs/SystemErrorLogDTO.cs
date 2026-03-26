using System;

namespace BusinessObjects.DTOs
{
    public class SystemErrorLogDTO
    {
        public int Id { get; set; }
        public string Level { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? StackTrace { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
