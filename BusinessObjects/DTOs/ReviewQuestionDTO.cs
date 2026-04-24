using System;

namespace BusinessObjects.DTOs
{
    public class ReviewQuestionDTO
    {
        public int Id { get; set; }
        public int CouncilId { get; set; }
        public byte ReviewRound { get; set; }
        public string? Category { get; set; }
        public string QuestionText { get; set; } = null!;
        public string? QuestionType { get; set; }
        public string? Priority { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ReviewQuestionResultDTO
    {
        public int QuestionId { get; set; }
        public int TeamId { get; set; }
        public byte ReviewRound { get; set; }
        public bool? YnValue { get; set; }
        public string? GradeValue { get; set; }
        public int SubmittedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }

    public class TeamReviewAssessmentTrackerDTO
    {
        public int TeamId { get; set; }
        public string TeamCode { get; set; } = null!;
        public bool Round1Passed { get; set; }
        public bool Round2Passed { get; set; }
        public bool Round3Passed { get; set; }
        public decimal? FinalGrade { get; set; }
        public string OverallStatus { get; set; } = "Pending"; // Pending, Passed, Failed
    }
}
