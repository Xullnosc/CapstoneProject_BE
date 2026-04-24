using System;

namespace BusinessObjects.DTOs
{
    public class ReviewScheduleDTO
    {
        public int Id { get; set; }
        public int CouncilId { get; set; }
        public byte ReviewRound { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? MeetLink { get; set; }
        public DateTime? NotifiedAt { get; set; }
        public int SetByLecturerId { get; set; }
        public string? SetByLecturerName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
