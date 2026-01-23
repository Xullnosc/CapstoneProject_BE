using System;

namespace BusinessObjects.DTOs
{
    public class TeamInvitationDTO
    {
        public int InvitationId { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamAvatar { get; set; }
        public int StudentId { get; set; }
        public int InvitedBy { get; set; }
        public string InviterName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
