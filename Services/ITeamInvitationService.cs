using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface ITeamInvitationService
    {
        Task<List<TeamInvitationDTO>> GetMyInvitationsAsync(int studentId);
        Task AcceptInvitationAsync(int invitationId, int studentId);
        Task DeclineInvitationAsync(int invitationId, int studentId);
        Task<TeamInvitationDTO> SendInvitationAsync(int teamId, int inviterId, string studentCodeOrEmail);
        Task CancelInvitationAsync(int invitationId, int userId);
    }
}
