using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class TeamMemberRepository : ITeamMemberRepository
    {
        private readonly ITeamMemberDAO _dao;

        public TeamMemberRepository(ITeamMemberDAO dao)
        {
            _dao = dao;
        }

        public async Task<Teammember> AddMemberAsync(Teammember member)
        {
            return await _dao.AddMemberAsync(member);
        }

        public async Task<bool> RemoveMemberAsync(int teamId, int studentId)
        {
            return await _dao.RemoveMemberAsync(teamId, studentId);
        }

        public async Task<List<Teammember>> GetMembersByTeamIdAsync(int teamId)
        {
            return await _dao.GetMembersByTeamIdAsync(teamId);
        }

        public async Task<Teammember?> GetMemberAsync(int teamId, int studentId)
        {
            return await _dao.GetMemberAsync(teamId, studentId);
        }

        public async Task<bool> IsStudentInTeamAsync(int studentId, int semesterId)
        {
            return await _dao.IsStudentInTeamAsync(studentId, semesterId);
        }
    }
}
