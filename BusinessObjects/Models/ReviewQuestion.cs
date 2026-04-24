using System;
using System.Collections.Generic;

namespace BusinessObjects.Models
{
    public partial class ReviewQuestion
    {
        public ReviewQuestion()
        {
            ReviewQuestionResults = new HashSet<ReviewQuestionResult>();
        }

        public int Id { get; set; }
        public int CouncilId { get; set; }
        public byte ReviewRound { get; set; }
        public string? Category { get; set; }
        public string QuestionText { get; set; } = null!;
        public string? QuestionType { get; set; } = "YesNo";
        public string? Priority { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual ReviewCouncil Council { get; set; } = null!;
        public virtual ICollection<ReviewQuestionResult> ReviewQuestionResults { get; set; }
    }
}
