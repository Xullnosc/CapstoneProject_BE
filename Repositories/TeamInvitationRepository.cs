using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class TeamInvitationRepository : ITeamInvitationRepository
    {
        private readonly ITeamInvitationDAO _dao;

        public TeamInvitationRepository(ITeamInvitationDAO dao)
        {
            _dao = dao;
        }

        public async Task<Teaminvitation> CreateAsync(Teaminvitation invitation)
        {
            return await _dao.CreateAsync(invitation);
        }

        public async Task<Teaminvitation?> GetByIdAsync(int invitationId)
        {
            return await _dao.GetByIdAsync(invitationId);
        }

        public async Task<List<Teaminvitation>> GetByStudentIdAsync(int studentId)
        {
            return await _dao.GetByStudentIdAsync(studentId);
        }

        public async Task<List<Teaminvitation>> GetByTeamIdAsync(int teamId)
        {
            return await _dao.GetByTeamIdAsync(teamId);
        }

        public async Task<bool> UpdateStatusAsync(int invitationId, string status)
        {
            return await _dao.UpdateStatusAsync(invitationId, status);
        }

        public async Task<List<Teaminvitation>> GetPendingInvitationsByStudentAsync(int studentId)
        {
            return await _dao.GetPendingInvitationsByStudentAsync(studentId);
        }

        public async Task CancelAllPendingInvitationsForStudentAsync(int studentId)
        {
            await _dao.CancelAllPendingInvitationsForStudentAsync(studentId);
        }

        public async Task<Teaminvitation?> GetByTeamAndStudentAsync(int teamId, int studentId)
        {
            return await _dao.GetByTeamAndStudentAsync(teamId, studentId);
        }
    }
}
