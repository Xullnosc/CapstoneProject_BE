using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class ReviewCouncilRepository : IReviewCouncilRepository
    {
        private readonly IReviewCouncilDAO _dao;

        public ReviewCouncilRepository(IReviewCouncilDAO dao)
        {
            _dao = dao;
        }

        public Task<List<ReviewCouncil>> GetCouncilsBySemesterAsync(int semesterId) => _dao.GetCouncilsBySemesterAsync(semesterId);
        public Task<ReviewCouncil?> GetCouncilByIdAsync(int councilId) => _dao.GetCouncilByIdAsync(councilId);
        public Task AddCouncilAsync(ReviewCouncil council) => _dao.AddCouncilAsync(council);
        public Task UpdateCouncilAsync(ReviewCouncil council) => _dao.UpdateCouncilAsync(council);
        public Task DeleteCouncilAsync(int councilId) => _dao.DeleteCouncilAsync(councilId);

        public Task AddMemberAsync(ReviewCouncilMember member) => _dao.AddMemberAsync(member);
        public Task RemoveMemberAsync(int councilId, int lecturerId) => _dao.RemoveMemberAsync(councilId, lecturerId);
        public Task AddTeamAsync(ReviewCouncilTeam team) => _dao.AddTeamAsync(team);
        public Task RemoveTeamAsync(int councilId, int teamId) => _dao.RemoveTeamAsync(councilId, teamId);
        public Task<ReviewCouncilTeam?> GetCouncilTeamAsync(int councilId, int teamId) => _dao.GetCouncilTeamAsync(councilId, teamId);
        public Task UpdateCouncilTeamAsync(ReviewCouncilTeam councilTeam) => _dao.UpdateCouncilTeamAsync(councilTeam);
    }
}
