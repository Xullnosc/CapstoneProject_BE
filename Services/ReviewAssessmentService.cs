using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;

namespace Services
{
    public class ReviewAssessmentService : IReviewAssessmentService
    {
        private readonly IReviewQuestionRepository _questionRepo;
        private readonly IReviewCouncilRepository _councilRepo;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IMapper _mapper;

        public ReviewAssessmentService(
            IReviewQuestionRepository questionRepo,
            IReviewCouncilRepository councilRepo,
            ISemesterRepository semesterRepository,
            IMapper mapper)
        {
            _questionRepo = questionRepo;
            _councilRepo = councilRepo;
            _semesterRepository = semesterRepository;
            _mapper = mapper;
        }

        public async Task<List<ReviewQuestionDTO>> GetQuestionsAsync(int councilId, byte round)
        {
            var questions = await _questionRepo.GetQuestionsAsync(councilId, round);
            return _mapper.Map<List<ReviewQuestionDTO>>(questions);
        }

        public async Task<List<ReviewQuestionResultDTO>> GetResultsAsync(int councilId, byte round, int teamId)
        {
            var results = await _questionRepo.GetResultsAsync(councilId, round, teamId);
            return _mapper.Map<List<ReviewQuestionResultDTO>>(results);
        }

        public async Task AddQuestionAsync(ReviewQuestionDTO questionDto)
        {
            var question = _mapper.Map<ReviewQuestion>(questionDto);
            await _questionRepo.AddQuestionAsync(question);
        }

        public async Task SaveQuestionsAsync(int councilId, byte round, List<ReviewQuestionDTO> questions)
        {
            foreach (var qDto in questions)
            {
                qDto.CouncilId = councilId;
                qDto.ReviewRound = round;
                await AddQuestionAsync(qDto);
            }
        }

        public async Task SubmitResultsAsync(List<ReviewQuestionResultDTO> resultDtos)
        {
            if (resultDtos == null || !resultDtos.Any()) return;

            // Check semester status
            var firstItem = resultDtos.First();
            // Need to find councilId from any of the questions
            var question = await _questionRepo.GetQuestionByIdAsync(firstItem.QuestionId);
            
            if (question != null)
            {
                var council = await _councilRepo.GetCouncilByIdAsync(question.CouncilId);
                if (council != null)
                {
                    var semester = await _semesterRepository.GetSemesterByIdSimpleAsync(council.SemesterId);
                    if (semester != null && !BusinessObjects.CampusConstants.SemesterStatus.IsLockedStage(semester.Status))
                    {
                        throw new InvalidOperationException("Assessments can only be submitted after the semester has been LOCKED.");
                    }
                }
            }

            var results = _mapper.Map<List<ReviewQuestionResult>>(resultDtos);
            await _questionRepo.SaveResultsAsync(results);
        }

        public async Task<TeamReviewAssessmentTrackerDTO> EvaluateTeamAsync(int councilId, int teamId)
        {
            var councilTeam = await _councilRepo.GetCouncilTeamAsync(councilId, teamId);
            if (councilTeam == null) throw new KeyNotFoundException("Team assignment in council not found");

            // Evaluate Round 1
            councilTeam.Round1Status = await CalculateRoundStatus(councilId, 1, teamId);
            // Evaluate Round 2
            councilTeam.Round2Status = await CalculateRoundStatus(councilId, 2, teamId);
            // Evaluate Round 3
            var round3Result = await CalculateRound3Grade(councilId, teamId);
            councilTeam.Round3Grade = round3Result.grade;
            councilTeam.Round3Status = round3Result.status;

            await _councilRepo.UpdateCouncilTeamAsync(councilTeam);

            return new TeamReviewAssessmentTrackerDTO
            {
                TeamId = teamId,
                Round1Passed = councilTeam.Round1Status == "Passed",
                Round2Passed = councilTeam.Round2Status == "Passed",
                Round3Passed = councilTeam.Round3Status == "Passed",
                FinalGrade = councilTeam.Round3Grade,
                OverallStatus = (councilTeam.Round1Status == "Passed" && councilTeam.Round2Status == "Passed" && councilTeam.Round3Status == "Passed") ? "Passed" : "Failed"
            };
        }

        public async Task OverrideTeamStatusAsync(int councilId, int teamId, byte round, string status, string comment)
        {
            var councilTeam = await _councilRepo.GetCouncilTeamAsync(councilId, teamId);
            if (councilTeam == null) throw new KeyNotFoundException("Council team not found");

            switch (round)
            {
                case 1: councilTeam.Round1Status = status; break;
                case 2: councilTeam.Round2Status = status; break;
                case 3: councilTeam.Round3Status = status; break;
            }

            councilTeam.IsOverride = true;
            councilTeam.OverallComment = comment;

            await _councilRepo.UpdateCouncilTeamAsync(councilTeam);
        }

        private async Task<string> CalculateRoundStatus(int councilId, byte round, int teamId)
        {
            var questions = await _questionRepo.GetQuestionsAsync(councilId, round);
            var results = await _questionRepo.GetResultsAsync(councilId, round, teamId);

            if (!results.Any()) return "Pending";

            // If any Mandatory question has YnValue = false, then Failed
            foreach (var q in questions.Where(x => x.Priority == "Mandatory"))
            {
                var res = results.FirstOrDefault(r => r.QuestionId == q.Id);
                if (res != null && res.YnValue == false) return "Failed";
            }

            // Check if all questions are answered? 
            // Minimal logic: if no Mandatory failed, then Passed
            return "Passed";
        }

        private async Task<(decimal? grade, string status)> CalculateRound3Grade(int councilId, int teamId)
        {
            var results = await _questionRepo.GetResultsAsync(councilId, 3, teamId);
            if (!results.Any()) return (null, "Pending");

            // Round 3 uses GradeValue
            var grades = results
                .Where(r => !string.IsNullOrEmpty(r.GradeValue) && decimal.TryParse(r.GradeValue, out _))
                .Select(r => decimal.Parse(r.GradeValue!))
                .ToList();

            if (!grades.Any()) return (null, "Pending");

            decimal avg = grades.Average();
            string status = avg >= 5.0m ? "Passed" : "Failed";

            return (avg, status);
        }
    }
}
