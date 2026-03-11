using BusinessObjects.Models;
using BusinessObjects;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class LecturerService : ILecturerService
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IRedisService _redisService;

        public LecturerService(
            ILecturerRepository lecturerRepository,
            IUserRepository userRepository,
            IWhitelistRepository whitelistRepository,
            ISemesterRepository semesterRepository,
            IRedisService redisService)
        {
            _lecturerRepository = lecturerRepository;
            _userRepository = userRepository;
            _whitelistRepository = whitelistRepository;
            _semesterRepository = semesterRepository;
            _redisService = redisService;
        }

        public async Task<IEnumerable<Lecturer>> GetAllLecturersAsync()
        {
            var result = await _lecturerRepository.GetAllAsync();
            var lecturers = result.ToList();
            
            // Populate avatars from User table if missing
            var emails = lecturers.Select(l => l.Email.Trim().ToLower()).Distinct().ToList();
            var users = await _userRepository.GetUsersByEmailsAsync(emails);
            var avatarDict = users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .GroupBy(u => u.Email!.Trim().ToLower())
                .ToDictionary(g => g.Key, g => g.First().Avatar);

            foreach (var l in lecturers)
            {
                l.Campus = CampusConstants.MapCodeToFullName(l.Campus);
                
                string emailKey = l.Email.Trim().ToLower();
                bool hasNoAvatar = string.IsNullOrWhiteSpace(l.Avatar) || l.Avatar == "N/A";
                
                if (hasNoAvatar && avatarDict.TryGetValue(emailKey, out var userAvatar) && !string.IsNullOrWhiteSpace(userAvatar))
                {
                    l.Avatar = userAvatar;
                }
            }
            
            return lecturers;
        }

        public async Task<Lecturer?> GetLecturerByIdAsync(int id)
        {
            return await _lecturerRepository.GetByIdAsync(id);
        }

        public async Task AddLecturerAsync(Lecturer lecturer)
        {
            // Normalize campus name
            lecturer.Campus = CampusConstants.MapCodeToFullName(lecturer.Campus);
            lecturer.CreatedAt = DateTime.UtcNow;
            lecturer.UpdatedAt = DateTime.UtcNow;
            
            await _lecturerRepository.AddAsync(lecturer);
            if (lecturer.IsActive == true)
            {
                await SyncLecturerWithWhitelists(lecturer, true);
            }
        }

        public async Task UpdateLecturerAsync(Lecturer lecturer)
        {
            var existing = await _lecturerRepository.GetByIdAsync(lecturer.LecturerId);
            if (existing == null) return;

            // Normalize campus name
            string? mappedCampus = CampusConstants.MapCodeToFullName(lecturer.Campus);

            bool statusChanged = existing.IsActive != lecturer.IsActive;
            bool infoChanged = existing.Email != lecturer.Email || 
                               existing.FullName != lecturer.FullName || 
                               existing.Campus != mappedCampus;

            // Update existing tracked entity
            existing.Email = lecturer.Email;
            existing.FullName = lecturer.FullName;
            existing.Avatar = lecturer.Avatar;
            existing.Campus = mappedCampus;
            existing.IsActive = lecturer.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _lecturerRepository.UpdateAsync(existing);

            if (statusChanged || (existing.IsActive == true && infoChanged))
            {
                await SyncLecturerWithWhitelists(existing, existing.IsActive == true);
            }
        }

        public async Task DeleteLecturerAsync(int id)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(id);
            if (lecturer != null)
            {
                await SyncLecturerWithWhitelists(lecturer, false);
                await _lecturerRepository.DeleteAsync(lecturer);
            }
        }

        public async Task ToggleLecturerStatusAsync(int id, bool isActive)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(id);
            if (lecturer != null && lecturer.IsActive != isActive)
            {
                lecturer.IsActive = isActive;
                await _lecturerRepository.UpdateAsync(lecturer);
                await SyncLecturerWithWhitelists(lecturer, isActive);
            }
        }

        private async Task SyncLecturerWithWhitelists(Lecturer lecturer, bool shouldBePresent)
        {
            var roles = await _semesterRepository.GetAllRolesAsync();
            var lecturerRole = roles.FirstOrDefault(r => r.RoleName == "Lecturer");
            if (lecturerRole == null) return;

            string? mappedCampus = CampusConstants.MapCodeToFullName(lecturer.Campus);

            // Fetch all whitelists globally with the lecturer role
            var globalWhitelists = await _whitelistRepository.GetByRoleAsync(lecturerRole.RoleId);
            
            // Look for an existing global entry for this email (where SemesterId is null)
            var existingEntry = globalWhitelists.FirstOrDefault(w => 
                w.Email.Equals(lecturer.Email, StringComparison.OrdinalIgnoreCase) && 
                w.SemesterId == null);

            bool changed = false;

            if (shouldBePresent)
            {
                if (existingEntry == null)
                {
                    await _whitelistRepository.AddAsync(new Whitelist
                    {
                        Email = lecturer.Email,
                        FullName = lecturer.FullName,
                        Avatar = lecturer.Avatar,
                        Campus = mappedCampus,
                        RoleId = lecturerRole.RoleId,
                        SemesterId = null, // Global lecturer whitelist
                        AddedDate = DateTime.UtcNow
                    });
                    changed = true;
                }
                else
                {
                    if (existingEntry.FullName != lecturer.FullName || 
                        existingEntry.Avatar != lecturer.Avatar || 
                        existingEntry.Campus != mappedCampus)
                    {
                        existingEntry.FullName = lecturer.FullName;
                        existingEntry.Avatar = lecturer.Avatar;
                        existingEntry.Campus = mappedCampus;
                        await _whitelistRepository.UpdateAsync(existingEntry);
                        changed = true;
                    }
                }
            }
            else if (existingEntry != null)
            {
                await _whitelistRepository.DeleteAsync(existingEntry);
                changed = true;
            }

            if (changed)
            {
                // Clear all semester caches since global lecturers might have changed
                await _redisService.RemoveByPrefixAsync("fctms:semester:");
            }
        }
    }
}
