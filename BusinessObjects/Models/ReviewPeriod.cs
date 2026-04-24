using System;

namespace BusinessObjects.Models
{
    public partial class ReviewPeriod
    {
        public int Id { get; set; }
        public int SemesterId { get; set; }
        public byte ReviewRound { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual Semester Semester { get; set; } = null!;
    }
}
