namespace Services.DTOs
{
    public class UpdatePasswordDTO
    {
        public string NewPassword { get; set; } = null!;
        public string? ConfirmPassword { get; set; }
    }
}
