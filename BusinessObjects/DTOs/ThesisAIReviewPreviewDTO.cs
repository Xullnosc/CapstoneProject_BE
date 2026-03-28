using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class ThesisAIReviewPreviewDTO
    {
        public string SuggestedDecision { get; set; } = "Consider";
        public string Feedback { get; set; } = string.Empty;
        public List<ThesisAIReviewChecklistItemDTO> Checklist { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool UsedMetadataFallback { get; set; }
        public string? Provider { get; set; }
        public string? Model { get; set; }
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class ThesisAIReviewChecklistItemDTO
    {
        public int ChecklistId { get; set; }
        public bool Checked { get; set; }
        public string? Reason { get; set; }
    }
}
