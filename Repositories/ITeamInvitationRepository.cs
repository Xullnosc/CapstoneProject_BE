using BusinessObjects.DTOs;
using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface ITeamInvitationRepository
    {
        Task<Teaminvitation> CreateAsync(Teaminvitation invitation);
        Task<Teaminvitation?> GetByIdAsync(int invitationId);
        Task<List<Teaminvitation>> GetByReceiverIdAsync(int studentId);
        Task<List<Teaminvitation>> GetByTeamIdAsync(int teamId);
        Task<bool> UpdateStatusAsync(int invitationId, string status);
        Task<List<Teaminvitation>> GetPendingInvitationsByReceiverAsync(int studentId);
        Task CancelAllPendingInvitationsForReceiverAsync(int studentId);
        Task<Teaminvitation?> GetByTeamAndReceiverAsync(int teamId, int studentId);

        // Mentor Invitation Methods
        Task CancelAllPendingMentorInvitationsForTeamAsync(int teamId);
        Task<List<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId);
        Task<PagedResult<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId, int pageIndex, int pageSize);
        Task<List<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId);
        Task<PagedResult<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId, int pageIndex, int pageSize);
        Task<Teaminvitation?> GetByTeamAndMentorAsync(int teamId, int mentorId);
        Task<int> GetMentorActiveTeamCountAsync(int mentorId, int semesterId);
    }
}

