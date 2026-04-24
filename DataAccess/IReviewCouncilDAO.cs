using BusinessObjects.Models;

namespace DataAccess
{
    public interface IReviewCouncilDAO
    {
        Task<List<ReviewCouncil>> GetCouncilsBySemesterAsync(int semesterId);
        Task<ReviewCouncil?> GetCouncilByIdAsync(int councilId);
        Task AddCouncilAsync(ReviewCouncil council);
        Task UpdateCouncilAsync(ReviewCouncil council);
        Task DeleteCouncilAsync(int councilId);
        
        Task AddMemberAsync(ReviewCouncilMember member);
        Task RemoveMemberAsync(int councilId, int lecturerId);
        Task AddTeamAsync(ReviewCouncilTeam team);
        Task RemoveTeamAsync(int councilId, int teamId);
        Task<ReviewCouncilTeam?> GetCouncilTeamAsync(int councilId, int teamId);
        Task UpdateCouncilTeamAsync(ReviewCouncilTeam councilTeam);
    }
}
