using System;

namespace BusinessObjects.DTOs
{
    public class TeamMemberDTO
    {
        public int TeamMemberId { get; set; }
        public int StudentId { get; set; }
        public string StudentCode { get; set; } // MSSV
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public string Role { get; set; } // Leader, Member
        public DateTime JoinedAt { get; set; }
    }
}
