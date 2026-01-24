using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class TeamRepository : ITeamRepository
    {
        private readonly ITeamDAO _teamDAO;

        public TeamRepository(ITeamDAO teamDAO)
        {
            _teamDAO = teamDAO;
        }

        public async Task<Team> CreateAsync(Team team)
        {
            return await _teamDAO.CreateAsync(team);
        }

        public async Task<Team?> GetByIdAsync(int teamId)
        {
            return await _teamDAO.GetByIdAsync(teamId);
        }

        public async Task<Team?> GetByCodeAsync(string teamCode)
        {
            return await _teamDAO.GetByCodeAsync(teamCode);
        }

        public async Task<List<Team>> GetBySemesterAsync(int semesterId)
        {
            return await _teamDAO.GetBySemesterAsync(semesterId);
        }

        public async Task<bool> UpdateStatusAsync(int teamId, string status)
        {
            return await _teamDAO.UpdateStatusAsync(teamId, status);
        }

        public async Task<int> CountTeamsInSemesterAsync(int semesterId)
        {
            return await _teamDAO.CountTeamsInSemesterAsync(semesterId);
        }

        public async Task<List<string>> GetTeamCodesBySemesterAsync(int semesterId)
        {
            return await _teamDAO.GetTeamCodesBySemesterAsync(semesterId);
        }

        public async Task<Team?> GetTeamByStudentIdAsync(int studentId, int semesterId)
        {
            return await _teamDAO.GetTeamByStudentIdAsync(studentId, semesterId);
        }
    }
}
