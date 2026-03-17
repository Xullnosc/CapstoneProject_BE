using System;

namespace BusinessObjects.DTOs
{
    public class ThesisHistoryDTO
    {
        public int Id { get; set; }
        public string FileUrl { get; set; } = null!;
        public int VersionNumber { get; set; }

        public DateTime CreatedAt { get; set; }
        public int UploadedBy { get; set; }
        public string? UploaderName { get; set; }
    }
}
