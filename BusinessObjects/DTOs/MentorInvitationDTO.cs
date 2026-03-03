using System;

namespace BusinessObjects.DTOs
{
    public class MentorInvitationDTO
    {
        public int InvitationId { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TeamCode { get; set; } = string.Empty;
        public int MentorId { get; set; } // Renamed from StudentId
        public string MentorEmail { get; set; } = string.Empty;
        public string MentorName { get; set; } = string.Empty;
        public int InvitedById { get; set; }
        public string InvitedByName { get; set; } = string.Empty;
        public string InvitedByEmail { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    /// <summary>
    /// Request body for POST /api/mentor-invitation/send
    /// </summary>
    public class SendMentorInvitationRequest
    {
        public int TeamId { get; set; }
        public string MentorEmail { get; set; } = string.Empty;
    }
}
