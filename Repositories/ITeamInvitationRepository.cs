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
    }
}
