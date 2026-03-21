namespace Services.DTOs
{
    public class ThesisApplicationDTO
    {
        public int Id { get; set; }
        public string ThesisId { get; set; } = null!;
        public string? ThesisTitle { get; set; }
        public string? ThesisOwnerName { get; set; }
        public int TeamId { get; set; }
        public string? TeamName { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }

    public class SubmitApplicationDTO
    {
        public string ThesisId { get; set; } = null!;
    }
}
