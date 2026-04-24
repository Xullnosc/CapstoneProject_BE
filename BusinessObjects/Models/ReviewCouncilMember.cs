namespace BusinessObjects.Models
{
    public partial class ReviewCouncilMember
    {
        public int CouncilId { get; set; }
        public int LecturerId { get; set; }
        public string Role { get; set; } = null!;

        public virtual ReviewCouncil Council { get; set; } = null!;
        public virtual Lecturer Lecturer { get; set; } = null!;
    }
}
