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
        Task<List<Teaminvitation>> GetByStudentIdAsync(int studentId);
        Task<List<Teaminvitation>> GetByTeamIdAsync(int teamId);
        Task<bool> UpdateStatusAsync(int invitationId, string status);
        Task<List<Teaminvitation>> GetPendingInvitationsByStudentAsync(int studentId);
        Task CancelAllPendingInvitationsForStudentAsync(int studentId);
        Task<Teaminvitation?> GetByTeamAndStudentAsync(int teamId, int studentId);

        // Mentor Invitation Methods
        Task<List<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId);
        Task<PagedResult<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId, int pageIndex, int pageSize);
        Task<List<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId);
        Task<PagedResult<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId, int pageIndex, int pageSize);
        Task<Teaminvitation?> GetByTeamAndMentorAsync(int teamId, int mentorId);
        Task<int> GetMentorActiveTeamCountAsync(int mentorId, int semesterId);
    }
}
