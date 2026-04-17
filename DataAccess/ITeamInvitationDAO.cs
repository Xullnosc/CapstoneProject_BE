using BusinessObjects.DTOs;
using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface ITeamInvitationDAO
    {
        Task<Teaminvitation> CreateAsync(Teaminvitation invitation);
        Task<Teaminvitation?> GetByIdAsync(int invitationId);
        Task<List<Teaminvitation>> GetByReceiverIdAsync(int receiverId);
        Task<PagedResult<Teaminvitation>> GetByReceiverIdAsync(int receiverId, int pageIndex, int pageSize);
        Task<List<Teaminvitation>> GetByTeamIdAsync(int teamId);
        Task<PagedResult<Teaminvitation>> GetByTeamIdAsync(int teamId, int pageIndex, int pageSize);
        Task<bool> UpdateStatusAsync(int invitationId, string status);
        Task<bool> DeleteAsync(int invitationId);
        Task<List<Teaminvitation>> GetPendingInvitationsByReceiverAsync(int receiverId);
        Task<PagedResult<Teaminvitation>> GetPendingInvitationsByReceiverAsync(int receiverId, int pageIndex, int pageSize);
        Task CancelAllPendingInvitationsForReceiverAsync(int receiverId);
        Task<Teaminvitation?> GetByTeamAndReceiverAsync(int teamId, int receiverId);

        // Mentor Invitation Methods
        Task<List<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId);
        Task<PagedResult<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId, int pageIndex, int pageSize);
        Task<List<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId);
        Task<PagedResult<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId, int pageIndex, int pageSize);
        Task<Teaminvitation?> GetByTeamAndMentorAsync(int teamId, int mentorId);
        Task CancelAllPendingMentorInvitationsForTeamAsync(int teamId);
        Task<int> GetMentorActiveTeamCountAsync(int mentorId, int semesterId);
    }
}
