using System;

namespace BusinessObjects.Models
{
    public partial class ReviewQuestionResult
    {
        public int QuestionId { get; set; }
        public int TeamId { get; set; }
        public byte ReviewRound { get; set; }
        public bool? YnValue { get; set; }
        public string? GradeValue { get; set; }
        public int SubmittedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public virtual ReviewQuestion Question { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual User Submitter { get; set; } = null!;
    }
}
