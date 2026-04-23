using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Models
{
    public class MidtermReview
    {
        [Key]
        public int MidtermReviewId { get; set; }

        public int SemesterId { get; set; }

        public DateTime LockDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("SemesterId")]
        public virtual Semester Semester { get; set; } = null!;
    }
}
