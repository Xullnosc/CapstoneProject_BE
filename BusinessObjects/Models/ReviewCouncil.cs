using System;
using System.Collections.Generic;

namespace BusinessObjects.Models
{
    public partial class ReviewCouncil
    {
        public ReviewCouncil()
        {
            ReviewCouncilMembers = new HashSet<ReviewCouncilMember>();
            ReviewCouncilTeams = new HashSet<ReviewCouncilTeam>();
            ReviewSchedules = new HashSet<ReviewSchedule>();
            ReviewQuestions = new HashSet<ReviewQuestion>();
        }

        public int Id { get; set; }
        public int SemesterId { get; set; }
        public string CouncilName { get; set; } = null!;
        public string Status { get; set; } = "Draft";
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Semester Semester { get; set; } = null!;
        public virtual ICollection<ReviewCouncilMember> ReviewCouncilMembers { get; set; }
        public virtual ICollection<ReviewCouncilTeam> ReviewCouncilTeams { get; set; }
        public virtual ICollection<ReviewSchedule> ReviewSchedules { get; set; }
        public virtual ICollection<ReviewQuestion> ReviewQuestions { get; set; }
    }
}
