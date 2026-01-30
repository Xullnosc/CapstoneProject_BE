namespace CapstoneProject_BE.DTOs.Requests
{
    public class SendInvitationRequest
    {
        public int TeamId { get; set; }
        public string StudentCodeOrEmail { get; set; } = string.Empty;
    }
}
