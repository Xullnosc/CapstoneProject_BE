public class ReviewPeriodDTO
{
    public int Id { get; set; }
    public int SemesterId { get; set; }
    public byte ReviewRound { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
