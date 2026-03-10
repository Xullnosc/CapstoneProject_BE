using AutoMapper;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class ThesisService : IThesisService
    {
        private readonly IThesisRepository _thesisRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryHelper _cloudinaryHelper;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IMapper _mapper;

        public ThesisService(
            IThesisRepository thesisRepository,
            ITeamRepository teamRepository,
            IUserRepository userRepository,
            ICloudinaryHelper cloudinaryHelper,
            ISemesterRepository semesterRepository,
            IMapper mapper)
        {
            _thesisRepository = thesisRepository;
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
            _semesterRepository = semesterRepository;
            _mapper = mapper;
        }

        // ─── Existing (not modified) ─────────────────────────────────────────────

        public async Task<Thesis> ProposeThesisAsync(ProposeThesisDTO req, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new Exception("User not found");

            // Prevent multiple theses per leader, except for Lecturers.
            // Allow re-proposing if all previous theses are Cancelled or Rejected.
            var existingTheses = await _thesisRepository.GetThesesByUserIdAsync(user.UserId);
            var hasActiveThesis = existingTheses.Any(t => t.Status != "Cancelled" && t.Status != "Rejected");
            if (hasActiveThesis && user.Role?.RoleName != CampusConstants.Roles.Lecturer)
            {
                throw new InvalidOperationException("You have already proposed a thesis. You cannot propose more than one.");
            }

            string? fileUrl = null;
            if (req.File != null)
                fileUrl = await _cloudinaryHelper.UploadFileAsync(req.File);

            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();

            var thesis = new Thesis
            {
                ThesisId = Guid.NewGuid().ToString(),
                Title = string.IsNullOrWhiteSpace(req.Title) 
                    ? System.IO.Path.GetFileNameWithoutExtension(req.File.FileName) 
                    : req.Title.Trim(),
                ShortDescription = req.ShortDescription,
                UserId = user.UserId,
                FileUrl = fileUrl,
                Status = "On Mentor Inviting",
                SemesterId = currentSemester?.SemesterId,
                UpDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            };

            return await _thesisRepository.CreateThesisAsync(thesis);
        }

        public async Task<IEnumerable<Thesis>> GetAllThesesAsync()
            => await _thesisRepository.GetAllThesesAsync();

        public async Task<Thesis?> GetThesisByIdAsync(string id)
            => await _thesisRepository.GetThesisByIdAsync(id);

        public async Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId)
            => await _thesisRepository.GetThesesByUserIdAsync(userId);

        public async Task UpdateThesisStatusAsync(string thesisId, string status)
        {
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new Exception("Thesis not found");

            thesis.Status = status;
            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);
        }

        // ─── Phase 02: New Methods ───────────────────────────────────────────────

        /// <summary>
        /// Upload a new file version for a thesis.
        /// - Only the owner can update their thesis.
        /// - Creates a ThesisHistory record for version tracking.
        /// - Updates FileUrl and UpdateDate on the original Thesis.
        /// </summary>
        public async Task<ThesisDTO> UpdateThesisAsync(string thesisId, UpdateThesisDTO req, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var thesis = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            // Only the owner can update
            if (thesis.UserId != user.UserId)
                throw new UnauthorizedAccessException("You are not authorized to update this thesis.");

            // Upload new file to Cloudinary (if provided)
            if (req.File != null)
            {
                string newFileUrl = await _cloudinaryHelper.UploadFileAsync(req.File);

                // Calculate next version number
                int nextVersion = thesis.ThesisHistories.Any()
                    ? thesis.ThesisHistories.Max(h => h.VersionNumber) + 1
                    : 1;

                // Create history record
                var history = new ThesisHistory
                {
                    ThesisId = thesis.ThesisId,
                    FileUrl = newFileUrl,
                    VersionNumber = nextVersion,
                    Note = req.Note?.Trim(),
                    UploadedBy = user.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _thesisRepository.AddThesisHistoryAsync(history);

                // Update thesis with new file URL
                thesis.FileUrl = newFileUrl;
            }

            // Update optional metadata fields
            if (!string.IsNullOrWhiteSpace(req.Title))
            {
                thesis.Title = req.Title.Trim();
            }
            else if (req.File != null)
            {
                // Sync Title with filename if a new file is uploaded and no title provided
                thesis.Title = System.IO.Path.GetFileNameWithoutExtension(req.File.FileName);
            }

            if (req.ShortDescription != null)
                thesis.ShortDescription = req.ShortDescription.Trim();

            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);

            // Reload with fresh histories
            var updated = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            return _mapper.Map<ThesisDTO>(updated!);
        }

        /// <summary>
        /// Cancel a thesis proposal.
        /// Only the owner can cancel it.
        /// </summary>
        public async Task<ThesisDTO> CancelThesisAsync(string thesisId, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var thesis = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            // Only the owner can cancel
            if (thesis.UserId != user.UserId)
                throw new UnauthorizedAccessException("You are not authorized to cancel this thesis.");

            // Only cancel if not already matched or published (can refine logic here if needed, usually just allow if it's 'Reviewing' or 'Registered')
            if (thesis.Status != "Reviewing" && thesis.Status != "Registered" && thesis.Status != "On Mentor Inviting")
                throw new InvalidOperationException($"Cannot cancel a thesis that is '{thesis.Status}'.");

            thesis.Status = "Cancelled";
            thesis.UpdateDate = DateTime.UtcNow;

            await _thesisRepository.UpdateThesisAsync(thesis);

            return _mapper.Map<ThesisDTO>(thesis);
        }

        /// <summary>
        /// Get all theses owned by the logged-in student.
        /// </summary>
        public async Task<IEnumerable<ThesisDTO>> GetMyThesesAsync(string email, string? status = null, string? searchTitle = null)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var ownerIds = new HashSet<int>();

            if (user.Role?.RoleName == CampusConstants.Roles.Lecturer)
            {
                // Always add their own ID so they can see their proposed theses
                ownerIds.Add(user.UserId);

                // Lecturer/Mentor view: see theses of all teams they mentor in current semester
                var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
                if (currentSemester != null)
                {
                    var allTeams = await _teamRepository.GetBySemesterAsync(currentSemester.SemesterId);
                    var mentoredLeaderIds = allTeams
                        .Where(t => (t.MentorId == user.UserId || t.MentorId2 == user.UserId) && t.Status != CampusConstants.TeamStatus.Disbanded)
                        .Select(t => t.LeaderId)
                        .ToList();

                    foreach (var id in mentoredLeaderIds) ownerIds.Add(id);
                }
            }
            else
            {
                // Student view: check if in a team
                var team = await _teamRepository.GetActiveTeamByStudentIdAsync(user.UserId);
                if (team != null)
                {
                    // Strictly see ONLY the leader's theses of their current team
                    ownerIds.Add(team.LeaderId);
                }
                else
                {
                    // If not in any team, see their own proposed theses
                    ownerIds.Add(user.UserId);
                }
            }

            if (!ownerIds.Any()) return new List<ThesisDTO>();

            var currentSem = await _semesterRepository.GetCurrentSemesterAsync();
            var theses = await _thesisRepository.GetThesesByUserIdsAsync(ownerIds, currentSem?.SemesterId);
            
            // Apply filtering in memory
            if (!string.IsNullOrWhiteSpace(status))
            {
                theses = theses.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                theses = theses.Where(t => t.Title != null && t.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase));
            }

            return _mapper.Map<IEnumerable<ThesisDTO>>(theses);
        }

        /// <summary>
        /// Get full thesis detail including version history.
        /// </summary>
        public async Task<ThesisDTO?> GetThesisDetailAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Thesis ID cannot be empty.");

            var thesis = await _thesisRepository.GetThesisByIdWithHistoriesAsync(id);
            if (thesis == null)
                return null;

            return _mapper.Map<ThesisDTO>(thesis);
        }

        /// <summary>
        /// Get filtered list of theses. All filters are optional.
        /// </summary>
        public async Task<IEnumerable<ThesisDTO>> GetFilteredThesesAsync(string? status, int? userId, string? searchTitle = null, int? semesterId = null)
        {
            var theses = await _thesisRepository.GetAllThesesFilteredAsync(status, userId, semesterId);
            var dtos = _mapper.Map<IEnumerable<ThesisDTO>>(theses);
            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                dtos = dtos.Where(d => d.Title != null && d.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase));
            }
            return dtos;
        }
    }
}
