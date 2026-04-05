using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class TeamDTO
    {
        public int TeamId { get; set; }
        public string TeamCode { get; set; }
        public string TeamName { get; set; }
        public string TeamAvatar { get; set; }
        public string Description { get; set; }
        public int SemesterId { get; set; }
        public int LeaderId { get; set; }
        public int? MentorId { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public string MentorEmail { get; set; } = string.Empty;
        public string MentorAvatar { get; set; } = string.Empty;

        public int? MentorId2 { get; set; }
        public string Mentor2Name { get; set; } = string.Empty;
        public string Mentor2Email { get; set; } = string.Empty;
        public string Mentor2Avatar { get; set; } = string.Empty;

        public string Status { get; set; }
        public int MemberCount { get; set; }
        public bool IsSpecial { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? TopicId { get; set; }
        public string? TopicName { get; set; }
        public string? TopicDescription { get; set; }
        public string? TopicStatus { get; set; }
        public string? TopicFileUrl { get; set; }

        public List<TeamMemberDTO> Members { get; set; }
    }

    public class MentorInfoDTO
    {
        public int MentorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }
}
