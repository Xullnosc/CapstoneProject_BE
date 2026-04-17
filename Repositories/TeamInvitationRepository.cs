using BusinessObjects.DTOs;
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

        public async Task<List<Teaminvitation>> GetByReceiverIdAsync(int studentId)
        {
            return await _dao.GetByReceiverIdAsync(studentId);
        }

        public async Task<List<Teaminvitation>> GetByTeamIdAsync(int teamId)
        {
            return await _dao.GetByTeamIdAsync(teamId);
        }

        public async Task<bool> UpdateStatusAsync(int invitationId, string status)
        {
            return await _dao.UpdateStatusAsync(invitationId, status);
        }

        public async Task<List<Teaminvitation>> GetPendingInvitationsByReceiverAsync(int studentId)
        {
            return await _dao.GetPendingInvitationsByReceiverAsync(studentId);
        }

        public async Task CancelAllPendingInvitationsForReceiverAsync(int studentId)
        {
            await _dao.CancelAllPendingInvitationsForReceiverAsync(studentId);
        }

        public async Task CancelAllPendingStudentInvitationsAsync(int studentId)
        {
            await _dao.CancelAllPendingStudentInvitationsAsync(studentId);
        }

        public async Task CancelAllPendingStudentInvitationsForTeamAsync(int teamId)
        {
            await _dao.CancelAllPendingStudentInvitationsForTeamAsync(teamId);
        }

        public async Task<Teaminvitation?> GetByTeamAndReceiverAsync(int teamId, int studentId)
        {
            return await _dao.GetByTeamAndReceiverAsync(teamId, studentId);
        }

        public async Task<Teaminvitation?> GetByTeamAndInviterAsync(int teamId, int inviterId)
        {
            return await _dao.GetByTeamAndInviterAsync(teamId, inviterId);
        }

        // --- Mentor Invitation Methods ---

        public Task CancelAllPendingMentorInvitationsForTeamAsync(int teamId) => _dao.CancelAllPendingMentorInvitationsForTeamAsync(teamId);

        public async Task<List<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId)
        {
            return await _dao.GetPendingMentorInvitationsByMentorIdAsync(mentorId);
        }

        public async Task<PagedResult<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId, int pageIndex, int pageSize)
        {
            return await _dao.GetPendingMentorInvitationsByMentorIdAsync(mentorId, pageIndex, pageSize);
        }

        public async Task<List<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId)
        {
            return await _dao.GetMentorInvitationsByTeamAsync(teamId);
        }

        public async Task<PagedResult<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId, int pageIndex, int pageSize)
        {
            return await _dao.GetMentorInvitationsByTeamAsync(teamId, pageIndex, pageSize);
        }

        public async Task<Teaminvitation?> GetByTeamAndMentorAsync(int teamId, int mentorId)
        {
            return await _dao.GetByTeamAndMentorAsync(teamId, mentorId);
        }

        public async Task<int> GetMentorActiveTeamCountAsync(int mentorId, int semesterId)
        {
            return await _dao.GetMentorActiveTeamCountAsync(mentorId, semesterId);
        }
    }
}

