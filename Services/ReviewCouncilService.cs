using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;

namespace Services
{
    public class ReviewCouncilService : IReviewCouncilService
    {
        private readonly IReviewCouncilRepository _repository;
        private readonly ITeamRepository _teamRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IMapper _mapper;

        public ReviewCouncilService(
            IReviewCouncilRepository repository,
            ITeamRepository teamRepository,
            ISemesterRepository semesterRepository,
            ILecturerRepository lecturerRepository,
            IMapper mapper)
        {
            _repository = repository;
            _teamRepository = teamRepository;
            _semesterRepository = semesterRepository;
            _lecturerRepository = lecturerRepository;
            _mapper = mapper;
        }

        public async Task<List<ReviewCouncilDTO>> GetCouncilsBySemesterAsync(int semesterId)
        {
            var councils = await _repository.GetCouncilsBySemesterAsync(semesterId);
            return _mapper.Map<List<ReviewCouncilDTO>>(councils);
        }

        public async Task<ReviewCouncilDTO?> GetCouncilByIdAsync(int councilId)
        {
            var council = await _repository.GetCouncilByIdAsync(councilId);
            if (council == null) return null;
            return _mapper.Map<ReviewCouncilDTO>(council);
        }

        public async Task<ReviewCouncilDTO> CreateCouncilAsync(int semesterId, string councilName, int createdBy)
        {
            var semester = await _semesterRepository.GetSemesterByIdSimpleAsync(semesterId);
            if (semester == null) throw new KeyNotFoundException("Semester not found.");

            if (!BusinessObjects.CampusConstants.SemesterStatus.IsLockedStage(semester.Status))
            {
                throw new InvalidOperationException("Councils can only be created after the semester has been LOCKED.");
            }

            var council = new ReviewCouncil
            {
                SemesterId = semesterId,
                CouncilName = councilName,
                CreatedBy = createdBy,
                Status = "Draft",
                CreatedAt = DateTime.Now
            };

            await _repository.AddCouncilAsync(council);
            return _mapper.Map<ReviewCouncilDTO>(council);
        }

        public async Task UpdateCouncilAsync(int councilId, string councilName, string status)
        {
            var council = await _repository.GetCouncilByIdAsync(councilId);
            if (council == null) throw new KeyNotFoundException("Council not found");

            council.CouncilName = councilName;
            council.Status = status;

            await _repository.UpdateCouncilAsync(council);
        }

        public async Task DeleteCouncilAsync(int councilId)
        {
            await _repository.DeleteCouncilAsync(councilId);
        }

        public async Task AddMemberToCouncilAsync(int councilId, int lecturerId, string role)
        {
            var council = await _repository.GetCouncilByIdAsync(councilId);
            if (council == null) throw new KeyNotFoundException("Council not found");

            // Exception Conflict: Verify member is not mentoring any team in this council
            var mentoredTeam = council.ReviewCouncilTeams
                .Select(ct => ct.Team)
                .FirstOrDefault(t => t != null && (t.MentorId == lecturerId || t.MentorId2 == lecturerId));

            if (mentoredTeam != null)
            {
                throw new InvalidOperationException($"Conflict: Lecturer cannot be in the council because they are mentoring Team {mentoredTeam.TeamCode}.");
            }

            // Check if already a member
            if (council.ReviewCouncilMembers.Any(m => m.LecturerId == lecturerId))
            {
                throw new InvalidOperationException("This lecturer is already a member of this council.");
            }

            var member = new ReviewCouncilMember
            {
                CouncilId = councilId,
                LecturerId = lecturerId,
                Role = role
            };

            await _repository.AddMemberAsync(member);
        }

        public async Task RemoveMemberFromCouncilAsync(int councilId, int lecturerId)
        {
            await _repository.RemoveMemberAsync(councilId, lecturerId);
        }

        public async Task AddTeamToCouncilAsync(int councilId, int teamId)
        {
            var council = await _repository.GetCouncilByIdAsync(councilId);
            if (council == null) throw new KeyNotFoundException("Council not found");

            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null) throw new KeyNotFoundException("Team not found");

            // Check if team already assigned to this council
            if (council.ReviewCouncilTeams.Any(t => t.TeamId == teamId))
            {
                throw new InvalidOperationException($"Team {team.TeamCode} is already assigned to this council.");
            }

            // Exception Conflict: Verify team's mentor is not in this council
            var conflictingMember = council.ReviewCouncilMembers
                .FirstOrDefault(m => m.LecturerId == team.MentorId || m.LecturerId == team.MentorId2);

            if (conflictingMember != null)
            {
                throw new InvalidOperationException($"Conflict: Team {team.TeamCode} cannot be added because their mentor is in this council.");
            }

            var councilTeam = new ReviewCouncilTeam
            {
                CouncilId = councilId,
                TeamId = teamId,
                AssignedAt = DateTime.Now
            };

            await _repository.AddTeamAsync(councilTeam);
        }

        public async Task RemoveTeamFromCouncilAsync(int councilId, int teamId)
        {
            await _repository.RemoveTeamAsync(councilId, teamId);
        }

        public async Task<List<ReviewCouncilDTO>> AutoGenerateCouncilsAsync(
            int semesterId, int reviewersPerCouncil, int createdBy)
        {
            var semester = await _semesterRepository.GetSemesterByIdSimpleAsync(semesterId);
            if (semester == null) throw new KeyNotFoundException("Semester not found.");

            if (!BusinessObjects.CampusConstants.SemesterStatus.IsLockedStage(semester.Status))
                throw new InvalidOperationException("Auto-generate is only available after the semester is LOCKED.");

            reviewersPerCouncil = Math.Max(1, reviewersPerCouncil);

            var allTeams     = await _teamRepository.GetBySemesterAsync(semesterId);
            // Midterm reviewer pool = ALL lecturers (not filtered by IsReviewer, which is for thesis review)
            var allReviewers = (await _lecturerRepository.GetAllAsync()).ToList();

            if (!allTeams.Any())     throw new InvalidOperationException("No teams found in this semester.");
            if (!allReviewers.Any()) throw new InvalidOperationException("No reviewers found.");

            // ── Step 1: Determine number of councils from reviewer pool ──
            int numCouncils = (int)Math.Ceiling((double)allReviewers.Count / reviewersPerCouncil);
            numCouncils = Math.Max(1, numCouncils);

            // ── Step 2: Distribute teams evenly, remainder goes to first councils ──
            int totalTeams   = allTeams.Count;
            int basePerCouncil  = totalTeams / numCouncils;   // every council gets at least this
            int remainder       = totalTeams % numCouncils;   // first `remainder` councils get +1

            // Build team slices
            var teamSlices = new List<List<BusinessObjects.Models.Team>>();
            int teamIndex = 0;
            for (int i = 0; i < numCouncils; i++)
            {
                int take = basePerCouncil + (i < remainder ? 1 : 0);
                teamSlices.Add(allTeams.GetRange(teamIndex, take));
                teamIndex += take;
            }

            // ── Step 3: Create councils, assign teams & reviewers ──
            var createdCouncils = new List<ReviewCouncilDTO>();
            var availableReviewers = allReviewers.ToList();

            for (int i = 0; i < numCouncils; i++)
            {
                var council = new BusinessObjects.Models.ReviewCouncil
                {
                    SemesterId = semesterId,
                    CouncilName = $"Council {(i + 1):D2}",
                    CreatedBy   = createdBy,
                    Status      = "Draft",
                    CreatedAt   = DateTime.Now
                };
                await _repository.AddCouncilAsync(council);

                var teamsForCouncil = teamSlices[i];

                // Assign teams
                foreach (var team in teamsForCouncil)
                {
                    await _repository.AddTeamAsync(new BusinessObjects.Models.ReviewCouncilTeam
                    {
                        CouncilId = council.Id,
                        TeamId    = team.TeamId,
                        AssignedAt = DateTime.Now
                    });
                }

                // Build set of mentor IDs for this council's teams
                var mentorIdsInCouncil = teamsForCouncil
                    .SelectMany(t => new[] { t.MentorId, t.MentorId2 })
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToHashSet();

                // Assign reviewers uniquely (Strict Requirement: No shared reviewers across councils)
                int assigned = 0;
                var toAssign = new List<BusinessObjects.Models.Lecturer>();

                foreach (var reviewer in availableReviewers)
                {
                    if (assigned >= reviewersPerCouncil) break;

                    bool isMentor = mentorIdsInCouncil.Contains(reviewer.LecturerId);
                    if (!isMentor)
                    {
                        toAssign.Add(reviewer);
                        assigned++;
                    }
                }

                foreach (var reviewer in toAssign)
                {
                    await _repository.AddMemberAsync(new BusinessObjects.Models.ReviewCouncilMember
                    {
                        CouncilId  = council.Id,
                        LecturerId = reviewer.LecturerId,
                        Role       = council.ReviewCouncilMembers.Count == 0 ? "Chairman" : "Midterm Reviewer"
                    });
                    availableReviewers.Remove(reviewer); // Remove from pool -> Global uniqueness
                }

                var reloaded = await _repository.GetCouncilByIdAsync(council.Id);
                if (reloaded != null)
                    createdCouncils.Add(_mapper.Map<ReviewCouncilDTO>(reloaded));
            }

            return createdCouncils;
        }
    }
}
