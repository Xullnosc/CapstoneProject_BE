using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface ITeamMemberRepository
    {
        Task<Teammember> AddMemberAsync(Teammember member);
        Task<bool> RemoveMemberAsync(int teamId, int studentId);
        Task<List<Teammember>> GetMembersByTeamIdAsync(int teamId);
        Task<Teammember?> GetMemberAsync(int teamId, int studentId);
        Task<bool> IsStudentInTeamAsync(int studentId, int semesterId);
    }
}
