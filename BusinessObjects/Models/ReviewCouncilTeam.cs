using System;

namespace BusinessObjects.Models
{
    public partial class ReviewCouncilTeam
    {
        public int CouncilId { get; set; }
        public int TeamId { get; set; }
        public DateTime AssignedAt { get; set; }
        
        public string? Round1Status { get; set; } = "Pending";
        public string? Round2Status { get; set; } = "Pending";
        public string? Round3Status { get; set; } = "Pending";
        public decimal? Round3Grade { get; set; }
        public bool IsOverride { get; set; } = false;
        public string? OverallComment { get; set; }

        public virtual ReviewCouncil Council { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
    }
}
