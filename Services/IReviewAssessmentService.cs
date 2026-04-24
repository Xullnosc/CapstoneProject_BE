using BusinessObjects.DTOs;

namespace Services
{
    public interface IReviewAssessmentService
    {
        Task<List<ReviewQuestionDTO>> GetQuestionsAsync(int councilId, byte round);
        Task<List<ReviewQuestionResultDTO>> GetResultsAsync(int councilId, byte round, int teamId);
        
        Task AddQuestionAsync(ReviewQuestionDTO question);
        Task SaveQuestionsAsync(int councilId, byte round, List<ReviewQuestionDTO> questions);

        Task SubmitResultsAsync(List<ReviewQuestionResultDTO> results);

        Task<TeamReviewAssessmentTrackerDTO> EvaluateTeamAsync(int councilId, int teamId);
        Task OverrideTeamStatusAsync(int councilId, int teamId, byte round, string status, string comment);
    }
}
