using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;

namespace Services
{
    public class ReviewScheduleService : IReviewScheduleService
    {
        private readonly IReviewScheduleRepository _scheduleRepo;
        private readonly IReviewCouncilRepository _councilRepo;
        private readonly IReviewPeriodRepository _periodRepo;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public ReviewScheduleService(
            IReviewScheduleRepository scheduleRepo, 
            IReviewCouncilRepository councilRepo,
            IReviewPeriodRepository periodRepo,
            ISemesterRepository semesterRepository,
            IMapper mapper,
            INotificationService notificationService)
        {
            _scheduleRepo = scheduleRepo;
            _councilRepo = councilRepo;
            _periodRepo = periodRepo;
            _semesterRepository = semesterRepository;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<List<ReviewScheduleDTO>> GetSchedulesByCouncilAsync(int councilId)
        {
            var list = await _scheduleRepo.GetSchedulesByCouncilAsync(councilId);
            return _mapper.Map<List<ReviewScheduleDTO>>(list);
        }

        public async Task<ReviewScheduleDTO> AddOrUpdateScheduleAsync(int councilId, byte reviewRound, DateTime scheduledDate, TimeSpan startTime, TimeSpan endTime, string meetLink, int setByLecturerId)
        {
            var council = await _councilRepo.GetCouncilByIdAsync(councilId);
            if (council == null) throw new KeyNotFoundException("Council not found");

            var semester = await _semesterRepository.GetSemesterByIdSimpleAsync(council.SemesterId);
            if (semester == null) throw new KeyNotFoundException("Semester not found.");

            if (!BusinessObjects.CampusConstants.SemesterStatus.IsLockedStage(semester.Status))
            {
                throw new InvalidOperationException("Review schedules can only be set after the semester has been LOCKED.");
            }

            // Look up period rules
            var period = await _periodRepo.GetPeriodAsync(council.SemesterId, reviewRound);
            if (period != null)
            {
                // Validate bounds
                if (scheduledDate.Date < period.StartDate.Date || scheduledDate.Date > period.EndDate.Date)
                {
                    throw new ArgumentException($"Scheduled date must be between {period.StartDate:dd/MM/yyyy} and {period.EndDate:dd/MM/yyyy} as configured by the HOD.");
                }
            }
            // If period == null, HOD hasn't set it yet. We can either throw or allow. 
            // Better to allow or throw. Let's throw to enforce period setup!
            else 
            {
                throw new InvalidOperationException($"HOD has not configured the Review Period dates for Round {reviewRound}. Please contact HOD.");
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException("Start time cannot be after end time.");
            }

            var schedule = new ReviewSchedule
            {
                CouncilId = councilId,
                ReviewRound = reviewRound,
                ScheduledDate = scheduledDate,
                StartTime = startTime,
                EndTime = endTime,
                MeetLink = meetLink,
                SetByLecturerId = setByLecturerId,
                NotifiedAt = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            await _scheduleRepo.AddOrUpdateScheduleAsync(schedule);

            // Send notification to all Lecturers in Council and all Leaders of Teams
            foreach (var member in council.ReviewCouncilMembers)
            {
                await _notificationService.CreateNotificationAsync(
                    member.LecturerId,
                    "System",
                    $"Review Schedule Updated: Round {reviewRound}",
                    $"Your council '{council.CouncilName}' has a review scheduled on {scheduledDate:dd/MM/yyyy} from {startTime} to {endTime}."
                );
            }

            foreach (var ct in council.ReviewCouncilTeams)
            {
                if (ct.Team != null && ct.Team.LeaderId > 0)
                {
                    await _notificationService.CreateNotificationAsync(
                        ct.Team.LeaderId,
                        "System",
                        $"Review Schedule Announced: Round {reviewRound}",
                        $"Your team's review is scheduled on {scheduledDate:dd/MM/yyyy} from {startTime} to {endTime}. Meeting Link: {meetLink}"
                    );
                }
            }

            var created = await _scheduleRepo.GetScheduleAsync(councilId, reviewRound);
            return _mapper.Map<ReviewScheduleDTO>(created);
        }
    }
}
