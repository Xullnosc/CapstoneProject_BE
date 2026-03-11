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
    public class ThesisFormService : IThesisFormService
    {
        private readonly IThesisFormRepository _thesisFormRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryHelper _cloudinaryHelper;

        public ThesisFormService(
            IThesisFormRepository thesisFormRepository,
            IUserRepository userRepository,
            ICloudinaryHelper cloudinaryHelper)
        {
            _thesisFormRepository = thesisFormRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
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
                UploaderName = form.UploadedByNavigation?.FullName,
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
                UploaderName = h.UploadedByNavigation?.FullName,
                CreatedAt = h.CreatedAt
            });
        }
    }
}
