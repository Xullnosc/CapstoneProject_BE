using System;

namespace BusinessObjects.DTOs
{
    public class ImportBatchDTO
    {
        public int ImportBatchId { get; set; }
        public string FileUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public int? AffectedSemesterId { get; set; }
        public int Version { get; set; }
        public string? Notes { get; set; }
    }
}
