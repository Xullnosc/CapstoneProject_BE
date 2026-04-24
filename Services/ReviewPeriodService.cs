using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using AutoMapper;

namespace Services
{
    public class ReviewPeriodService : IReviewPeriodService
    {
        private readonly IReviewPeriodRepository _repository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IMapper _mapper;

        public ReviewPeriodService(IReviewPeriodRepository repository, ISemesterRepository semesterRepository, IMapper mapper)
        {
            _repository = repository;
            _semesterRepository = semesterRepository;
            _mapper = mapper;
        }

        public async Task<List<ReviewPeriodDTO>> GetPeriodsBySemesterAsync(int semesterId)
        {
            var periods = await _repository.GetPeriodsBySemesterAsync(semesterId);
            return _mapper.Map<List<ReviewPeriodDTO>>(periods);
        }

        public async Task<ReviewPeriodDTO> AddOrUpdatePeriodAsync(int semesterId, byte reviewRound, DateTime startDate, DateTime endDate)
        {
            var semester = await _semesterRepository.GetSemesterByIdSimpleAsync(semesterId);
            if (semester == null) throw new KeyNotFoundException("Semester not found.");

            if (!BusinessObjects.CampusConstants.SemesterStatus.IsLockedStage(semester.Status))
            {
                throw new InvalidOperationException("Mid-term Review periods can only be configured after the semester has been LOCKED by the HOD.");
            }

            if (startDate > endDate)
                throw new ArgumentException("Start date cannot be after end date.");

            var period = new ReviewPeriod
            {
                SemesterId = semesterId,
                ReviewRound = reviewRound,
                StartDate = startDate,
                EndDate = endDate
            };

            await _repository.AddOrUpdatePeriodAsync(period);
            
            // Re-fetch to get the DTO (with generated ID if new)
            var created = await _repository.GetPeriodAsync(semesterId, reviewRound);
            return _mapper.Map<ReviewPeriodDTO>(created);
        }
    }
}
