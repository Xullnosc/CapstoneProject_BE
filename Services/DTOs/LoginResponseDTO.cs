namespace Services.DTOs
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = null!;
        public UserInfoDTO UserInfo { get; set; } = null!;
    }
}
