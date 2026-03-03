using BusinessObjects.DTOs;
using System.Threading.Tasks;

namespace Services
{
    public interface IMentorInvitationService
    {
        Task<PagedResult<MentorInvitationDTO>> GetMentorInvitationsAsync(int mentorId, int pageIndex, int pageSize);
        Task<PagedResult<MentorInvitationDTO>> GetTeamMentorInvitationsAsync(int teamId, int leaderId, int pageIndex, int pageSize);
        Task<MentorInvitationDTO> SendMentorInvitationAsync(int teamId, int leaderId, string mentorEmail);
        Task AcceptMentorInvitationAsync(int invitationId, int mentorId);
        Task DeclineMentorInvitationAsync(int invitationId, int mentorId);
        Task CancelMentorInvitationAsync(int invitationId, int leaderId);
        Task<int> GetMentorActiveTeamCountAsync(int mentorId);
    }
}
