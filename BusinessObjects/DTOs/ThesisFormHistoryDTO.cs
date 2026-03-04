using System;

namespace BusinessObjects.DTOs
{
    public class ThesisFormHistoryDTO
    {
        public int Id { get; set; }
        public int ThesisFormId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public int UploadedBy { get; set; }
        public string? UploaderName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
