using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.Extensions.Logging;
using Repositories;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class ThesisFormService : IThesisFormService
    {
        private readonly IThesisFormRepository _thesisFormRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryHelper _cloudinaryHelper;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ThesisFormService> _logger;

        public ThesisFormService(
            IThesisFormRepository thesisFormRepository,
            IUserRepository userRepository,
            ICloudinaryHelper cloudinaryHelper,
            ISemesterRepository semesterRepository,
            ITeamRepository teamRepository,
            INotificationService notificationService,
            ILogger<ThesisFormService> logger)
        {
            _thesisFormRepository = thesisFormRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
            _semesterRepository = semesterRepository;
            _teamRepository = teamRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<ThesisFormDTO> UploadThesisFormAsync(UploadThesisFormDTO req, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.Role?.RoleName != CampusConstants.Roles.HOD)
            {
                throw new UnauthorizedAccessException("Only the Head of Department can upload a Thesis Form.");
            }

            if (req.File == null)
            {
                throw new ArgumentException("No file provided.");
            }

            var fileUrl = await _cloudinaryHelper.UploadFileAsync(req.File);

            var existingForm = await _thesisFormRepository.GetLatestFormAsync();

            int versionNumber = 1;
            int formId = 0;

            if (existingForm == null)
            {
                var newForm = new ThesisForm
                {
                    FileUrl = fileUrl,
                    VersionNumber = 1,
                    UploadedBy = user.UserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                newForm = await _thesisFormRepository.AddFormAsync(newForm);
                formId = newForm.Id;
            }
            else
            {
                versionNumber = existingForm.VersionNumber + 1;
                existingForm.FileUrl = fileUrl;
                existingForm.VersionNumber = versionNumber;
                existingForm.UploadedBy = user.UserId;
                existingForm.UpdatedAt = DateTime.UtcNow;

                // Clear navigation properties to prevent EF Core tracking conflict 
                // since existingForm was fetched AsNoTracking 
                // and the Context is already tracking the user object.
                existingForm.Uploader = null!;
                existingForm.Histories = null!;

                await _thesisFormRepository.UpdateFormAsync(existingForm);
                formId = existingForm.Id;
            }

            var history = new ThesisFormHistory
            {
                ThesisFormId = formId,
                FileUrl = fileUrl,
                VersionNumber = versionNumber,
                UploadedBy = user.UserId,
                CreatedAt = DateTime.UtcNow
            };
            await _thesisFormRepository.AddFormHistoryAsync(history);

            await NotifyActiveSemesterUsersAsync(
                NotificationType.HODAction.ToString(),
                "New thesis form version published",
                $"A new thesis form version (v{versionNumber}) has been published by HOD. Please review it.",
                "ThesisForm",
                formId);

            return new ThesisFormDTO
            {
                Id = formId,
                FileUrl = fileUrl,
                VersionNumber = versionNumber,
                UploadedBy = user.UserId,
                UploaderName = user.FullName,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<ThesisFormDTO?> GetLatestFormAsync()
        {
            var form = await _thesisFormRepository.GetLatestFormAsync();
            if (form == null) return null;

            return new ThesisFormDTO
            {
                Id = form.Id,
                FileUrl = form.FileUrl,
                VersionNumber = form.VersionNumber,
                UploadedBy = form.UploadedBy,
                UploaderName = form.Uploader?.FullName,
                UpdatedAt = form.UpdatedAt
            };
        }

        public async Task<IEnumerable<ThesisFormHistoryDTO>> GetFormHistoriesAsync()
        {
            var histories = await _thesisFormRepository.GetFormHistoriesAsync();
            return histories.Select(h => new ThesisFormHistoryDTO
            {
                Id = h.Id,
                ThesisFormId = h.ThesisFormId,
                FileUrl = h.FileUrl,
                VersionNumber = h.VersionNumber,
                UploadedBy = h.UploadedBy,
                UploaderName = h.Uploader?.FullName,
                CreatedAt = h.CreatedAt
            });
        }

        private async Task NotifyActiveSemesterUsersAsync(string type, string title, string message, string relatedEntityType, int relatedEntityId)
        {
            try
            {
                var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
                if (currentSemester == null)
                {
                    return;
                }

                var teams = await _teamRepository.GetBySemesterAsync(currentSemester.SemesterId);
                var recipientIds = teams
                    .SelectMany(team =>
                        team.Teammembers.Select(member => member.StudentId)
                            .Concat(team.MentorId.HasValue ? new[] { team.MentorId.Value } : Enumerable.Empty<int>())
                            .Concat(team.MentorId2.HasValue ? new[] { team.MentorId2.Value } : Enumerable.Empty<int>()))
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (recipientIds.Count == 0)
                {
                    return;
                }

                await _notificationService.CreateBulkNotificationsAsync(
                    recipientIds,
                    type,
                    title,
                    message,
                    relatedEntityType,
                    relatedEntityId,
                    sendEmail: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify users for thesis form event.");
            }
        }
    }
}
