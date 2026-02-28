using AutoMapper;
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
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryHelper _cloudinaryHelper;
        private readonly IMapper _mapper;

        public ThesisService(
            IThesisRepository thesisRepository,
            IUserRepository userRepository,
            ICloudinaryHelper cloudinaryHelper,
            IMapper mapper)
        {
            _thesisRepository = thesisRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
            _mapper = mapper;
        }

        // ─── Existing (not modified) ─────────────────────────────────────────────

        public async Task<Thesis> ProposeThesisAsync(ProposeThesisDTO req, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new Exception("User not found");

            string? fileUrl = null;
            if (req.File != null)
                fileUrl = await _cloudinaryHelper.UploadFileAsync(req.File);

            var thesis = new Thesis
            {
                ThesisId = Guid.NewGuid().ToString(),
                Title = req.Title,
                ShortDescription = req.ShortDescription,
                UserId = user.UserId,
                FileUrl = fileUrl,
                Status = "Reviewing",
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
                thesis.Title = req.Title.Trim();

            if (req.ShortDescription != null)
                thesis.ShortDescription = req.ShortDescription.Trim();

            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);

            // Reload with fresh histories
            var updated = await _thesisRepository.GetThesisByIdWithHistoriesAsync(thesisId);
            return _mapper.Map<ThesisDTO>(updated!);
        }

        /// <summary>
        /// Get all theses owned by the logged-in student.
        /// </summary>
        public async Task<IEnumerable<ThesisDTO>> GetMyThesesAsync(string email, string? status = null, string? searchTitle = null)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var theses = await _thesisRepository.GetThesesByUserIdAsync(user.UserId);
            
            // Apply filtering in memory
            if (!string.IsNullOrWhiteSpace(status))
            {
                theses = theses.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                theses = theses.Where(t => t.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase));
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
        public async Task<IEnumerable<ThesisDTO>> GetFilteredThesesAsync(string? status, int? userId, string? searchTitle = null)
        {
            var theses = await _thesisRepository.GetAllThesesFilteredAsync(status, userId);
            var dtos = _mapper.Map<IEnumerable<ThesisDTO>>(theses);
            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                dtos = dtos.Where(d => d.Title != null && d.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase));
            }
            return dtos;
        }
    }
}
