using System;

namespace BusinessObjects.Models
{
    public class SystemErrorLog
    {
        public int Id { get; set; }
        public string Level { get; set; } = null!; // Error, Warning, Info
        public string Message { get; set; } = null!;
        public string? StackTrace { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
