using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class ReviewCouncilDTO
    {
        public int CouncilId { get; set; }        // renamed from Id
        public int SemesterId { get; set; }
        public string CouncilName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<ReviewCouncilMemberDTO> Members { get; set; } = new();
        public List<ReviewCouncilTeamDTO> Teams { get; set; } = new();
    }

    public class ReviewCouncilMemberDTO
    {
        public int LecturerId { get; set; }
        public string Role { get; set; } = null!;
        public string? LecturerName { get; set; }
        public string? LecturerEmail { get; set; }
    }

    public class ReviewCouncilTeamDTO
    {
        public int TeamId { get; set; }
        public string? TeamCode { get; set; }
        public string? TeamName { get; set; }
        public string? MentorName { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    public class CreateReviewCouncilDTO
    {
        public int SemesterId { get; set; }
        public string CouncilName { get; set; } = null!;
        public int CreatedBy { get; set; }
    }

    public class UpdateReviewCouncilDTO
    {
        public string CouncilName { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public class AddCouncilMemberDTO
    {
        public int LecturerId { get; set; }
        public string Role { get; set; } = null!;
    }

    public class AddCouncilTeamDTO
    {
        public int TeamId { get; set; }
    }

    public class AutoGenerateCouncilsDTO
    {
        public int SemesterId { get; set; }
        public int ReviewersPerCouncil { get; set; } = 2;
    }
}
