using System;

namespace BusinessObjects.DTOs
{
    public class TeamInvitationDTO
    {
        public int InvitationId { get; set; }
        public int TeamId { get; set; }
        public TeamInfoDTO Team { get; set; } // Nested Team Object
        public int StudentId { get; set; }
        public InvitedByDTO InvitedBy { get; set; } // Nested Inviter Object
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TeamInfoDTO
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamAvatar { get; set; }
        public int MemberCount { get; set; }
        public string LeaderName { get; set; }
    }

    public class InvitedByDTO
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
    }
}
