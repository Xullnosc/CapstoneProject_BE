using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;

namespace Services
{
    public interface ITeamService
    {
        Task<TeamDTO> CreateTeamAsync(int leaderId, CreateTeamDTO createTeamDto);
        Task<TeamDTO?> GetTeamByIdAsync(int teamId, int userId);
        Task<List<TeamDTO>> GetTeamsBySemesterAsync(int semesterId);
        Task<PagedResult<TeamDTO>> GetTeamsBySemesterPagedAsync(int semesterId, int page, int limit);
        Task<bool> DisbandTeamAsync(int teamId, int leaderId);
        Task<TeamDTO?> GetTeamByStudentIdAsync(int studentId);
        Task<TeamDTO> UpdateTeamAsync(int teamId, int leaderId, BusinessObjects.DTOs.UpdateTeamDTO updateTeamDto);
        Task<bool> RemoveMemberAsync(int teamId, int studentId);
    }
}
