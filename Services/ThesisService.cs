using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class ThesisService : IThesisService
    {
        private readonly IThesisRepository _thesisRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryHelper _cloudinaryHelper;

        public ThesisService(
            IThesisRepository thesisRepository,
            IUserRepository userRepository,
            ICloudinaryHelper cloudinaryHelper)
        {
            _thesisRepository = thesisRepository;
            _userRepository = userRepository;
            _cloudinaryHelper = cloudinaryHelper;
        }

        public async Task<Thesis> ProposeThesisAsync(ProposeThesisDTO req, string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Upload the proposal document to Cloudinary
            string fileUrl = null;
            if (req.File != null)
            {
                fileUrl = await _cloudinaryHelper.UploadFileAsync(req.File);
            }

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
        {
            return await _thesisRepository.GetAllThesesAsync();
        }

        public async Task<Thesis?> GetThesisByIdAsync(string id)
        {
            return await _thesisRepository.GetThesisByIdAsync(id);
        }

        public async Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId)
        {
            return await _thesisRepository.GetThesesByUserIdAsync(userId);
        }

        public async Task UpdateThesisStatusAsync(string thesisId, string status)
        {
            var thesis = await _thesisRepository.GetThesisByIdAsync(thesisId);
            if (thesis == null)
            {
                throw new Exception("Thesis not found");
            }

            thesis.Status = status;
            thesis.UpdateDate = DateTime.UtcNow;
            await _thesisRepository.UpdateThesisAsync(thesis);
        }
    }
}
