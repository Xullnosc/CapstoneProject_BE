using BusinessObjects.DTOs;

namespace Services
{
    public interface IReviewCouncilService
    {
        Task<List<ReviewCouncilDTO>> GetCouncilsBySemesterAsync(int semesterId);
        Task<ReviewCouncilDTO?> GetCouncilByIdAsync(int councilId);
        Task<ReviewCouncilDTO> CreateCouncilAsync(int semesterId, string councilName, int createdBy);
        Task UpdateCouncilAsync(int councilId, string councilName, string status);
        Task DeleteCouncilAsync(int councilId);

        Task AddMemberToCouncilAsync(int councilId, int lecturerId, string role);
        Task RemoveMemberFromCouncilAsync(int councilId, int lecturerId);

        Task AddTeamToCouncilAsync(int councilId, int teamId);
        Task RemoveTeamFromCouncilAsync(int councilId, int teamId);

        Task<List<ReviewCouncilDTO>> AutoGenerateCouncilsAsync(int semesterId, int reviewersPerCouncil, int createdBy);
    }
}
