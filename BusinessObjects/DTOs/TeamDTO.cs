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
        public string Status { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TeamMemberDTO> Members { get; set; }
    }
}
