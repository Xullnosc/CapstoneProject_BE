using BusinessObjects.Models;
using BusinessObjects;
using BusinessObjects.DTOs;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Interfaces;

namespace Services
{
    public class LecturerService : ILecturerService
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IRedisService _redisService;
        private readonly ICampusContextService _campusContextService;
        private readonly ISystemUserCredentialRepository _credentialRepository;

        public LecturerService(
            ILecturerRepository lecturerRepository,
            IUserRepository userRepository,
            IWhitelistRepository whitelistRepository,
            ISemesterRepository semesterRepository,
            IRedisService redisService,
            ICampusContextService campusContextService,
            ISystemUserCredentialRepository credentialRepository)
        {
            _lecturerRepository = lecturerRepository;
            _userRepository = userRepository;
            _whitelistRepository = whitelistRepository;
            _semesterRepository = semesterRepository;
            _redisService = redisService;
            _campusContextService = campusContextService;
            _credentialRepository = credentialRepository;
        }

        public async Task<IEnumerable<Lecturer>> GetAllLecturersAsync()
        {
            var result = await _lecturerRepository.GetAllAsync();
            var lecturers = result.ToList();

            if (!lecturers.Any())
            {
                return lecturers;
            }
            
            // Populate avatars from User table if missing
            var emails = lecturers.Select(l => l.Email.Trim().ToLowerInvariant()).Distinct().ToList();
            var users = await _userRepository.GetUsersByEmailsAsync(emails);
            var avatarDict = users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .GroupBy(u => u.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Avatar, StringComparer.OrdinalIgnoreCase);

            foreach (var l in lecturers)
            {
                string emailKey = l.Email.Trim();
                bool hasNoAvatar = string.IsNullOrWhiteSpace(l.Avatar) || l.Avatar == "N/A";
                
                if (hasNoAvatar && avatarDict.TryGetValue(emailKey, out var userAvatar) && !string.IsNullOrWhiteSpace(userAvatar))
                {
                    l.Avatar = userAvatar;
                }
            }
            
            return lecturers;
        }

        public async Task<BusinessObjects.DTOs.PagedResult<Lecturer>> GetLecturersPaginatedAsync(int page, int pageSize, string? search = null)
        {
            var all = await _lecturerRepository.GetAllAsync();
            var query = all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(l => 
                    (l.Email != null && l.Email.ToLower().Contains(s)) ||
                    (l.FullName != null && l.FullName.ToLower().Contains(s))
                );
            }

            var list = query.OrderBy(l => l.FullName ?? l.Email).ToList();
            int total = list.Count;
            var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Populate avatars for the current page
            var emails = items.Select(l => l.Email.Trim()).Distinct().ToList();
            var users = await _userRepository.GetUsersByEmailsAsync(emails);
            var avatarDict = users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .GroupBy(u => u.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Avatar, StringComparer.OrdinalIgnoreCase);

            foreach (var l in items)
            {
                if (string.IsNullOrWhiteSpace(l.Avatar) || l.Avatar == "N/A")
                {
                    if (avatarDict.TryGetValue(l.Email.Trim(), out var avatar))
                    {
                        l.Avatar = avatar;
                    }
                }
            }

            return new BusinessObjects.DTOs.PagedResult<Lecturer>(items, total, page, pageSize);
        }
        
        public async Task<PagedResult<Lecturer>> GetLecturersByCampusAsync(string campus, int pageIndex, int pageSize)
        {
            var pagedResult = await _lecturerRepository.GetByCampusAsync(campus, pageIndex, pageSize);
            var lecturers = pagedResult.Items;

            if (!lecturers.Any())
            {
                return pagedResult;
            }

            var emails = lecturers.Select(l => l.Email.Trim().ToLowerInvariant()).Distinct().ToList();
            var users = await _userRepository.GetUsersByEmailsAsync(emails);
            var avatarDict = users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .GroupBy(u => u.Email!.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First().Avatar);

            foreach (var l in lecturers)
            {
                string emailKey = l.Email.Trim().ToLowerInvariant();
                bool hasNoAvatar = string.IsNullOrWhiteSpace(l.Avatar) || l.Avatar == "N/A";

                if (hasNoAvatar && avatarDict.TryGetValue(emailKey, out var userAvatar) && !string.IsNullOrWhiteSpace(userAvatar))
                {
                    l.Avatar = userAvatar;
                }
            }

            pagedResult.Items = lecturers;
            return pagedResult;
        }

        public async Task<Lecturer?> GetLecturerByIdAsync(int id)
        {
            return await _lecturerRepository.GetByIdAsync(id);
        }

        public async Task AddLecturerAsync(Lecturer lecturer)
        {
            // Normalize data
            if (string.IsNullOrWhiteSpace(lecturer.Email))
            {
                throw new InvalidOperationException("Email giảng viên là bắt buộc.");
            }
            lecturer.Email = lecturer.Email.Trim();

            // Check if lecturer already exists
            var existing = await _lecturerRepository.GetByEmailAsync(lecturer.Email);
            if (existing != null)
            {
                var campusName = CampusConstants.MapIdToFullName(existing.CampusId);
                throw new InvalidOperationException($"Giảng viên này đã tồn tại ở cơ sở {campusName}.");
            }

            // Security check & support manual CampusId
            var contextCampusId = _campusContextService.GetCurrentCampusId();
            if (contextCampusId != null)
            {
                // If requester has a campus context (HOD), force it
                lecturer.CampusId = contextCampusId.Value;
            }
            else if (lecturer.CampusId <= 0)
            {
                // If requester is Super Admin and didn't provide CampusId, throw
                throw new InvalidOperationException("Super Admin phải cung cấp CampusId hợp lệ.");
            }

            lecturer.CreatedAt = DateTime.UtcNow;
            lecturer.UpdatedAt = DateTime.UtcNow;
            
            await _lecturerRepository.AddAsync(lecturer);
            if (lecturer.IsActive)
            {
                await SyncLecturerWithWhitelists(lecturer, true);
            }
        }

        public async Task UpdateLecturerAsync(Lecturer lecturer)
        {
            var existing = await _lecturerRepository.GetByIdAsync(lecturer.LecturerId);
            if (existing == null) return;

            // Normalize data
            if (lecturer.Email != null) lecturer.Email = lecturer.Email.Trim();

            bool statusChanged = existing.IsActive != lecturer.IsActive;
            string? oldEmail = existing.Email;
            bool infoChanged = (oldEmail != null && !oldEmail.Equals(lecturer.Email, StringComparison.OrdinalIgnoreCase)) || 
                               (existing.FullName != null && !existing.FullName.Equals(lecturer.FullName, StringComparison.OrdinalIgnoreCase));

            // Update existing tracked entity
            existing.Email = lecturer.Email;
            existing.FullName = lecturer.FullName;
            existing.Avatar = lecturer.Avatar;
            existing.IsActive = lecturer.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _lecturerRepository.UpdateAsync(existing);

            if (statusChanged || (existing.IsActive && infoChanged))
            {
                await SyncLecturerWithWhitelists(existing, existing.IsActive, oldEmail);
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

        public async Task ToggleReviewerAsync(int id, bool isReviewer)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(id);
            if (lecturer != null && lecturer.IsReviewer != isReviewer)
            {
                lecturer.IsReviewer = isReviewer;
                await _lecturerRepository.UpdateAsync(lecturer);
            }
        }

        private async Task SyncLecturerWithWhitelists(Lecturer lecturer, bool shouldBePresent, string? oldEmail = null)
        {
            if (string.IsNullOrWhiteSpace(lecturer.Email)) return;

            var roles = await _semesterRepository.GetAllRolesAsync();
            var lecturerRole = roles.FirstOrDefault(r => string.Equals(r.RoleName, CampusConstants.Roles.Lecturer, StringComparison.OrdinalIgnoreCase));
            if (lecturerRole == null) return;

            // Fetch all whitelists globally with the lecturer role
            var globalWhitelists = await _whitelistRepository.GetByRoleAsync(lecturerRole.RoleId);
            
            bool changed = false;

            // Handle email change if oldEmail provided
            if (!string.IsNullOrEmpty(oldEmail) && !oldEmail.Equals(lecturer.Email, StringComparison.OrdinalIgnoreCase))
            {
                // 1. Update all Whitelist entries matching oldEmail
                var whitelistsToUpdate = globalWhitelists.Where(w => 
                    !string.IsNullOrEmpty(w.Email) && 
                    w.Email.Trim().Equals(oldEmail.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                
                foreach (var w in whitelistsToUpdate)
                {
                    w.Email = lecturer.Email;
                    w.FullName = lecturer.FullName; // Sync info too
                    w.Avatar = lecturer.Avatar;
                    await _whitelistRepository.UpdateAsync(w);
                }

                // 2. Update User table
                var user = await _userRepository.GetByEmailAsync(oldEmail);
                if (user != null)
                {
                    user.Email = lecturer.Email;
                    user.FullName = lecturer.FullName;
                    user.Avatar = lecturer.Avatar;
                    await _userRepository.UpdateAsync(user);

                    // 3. Update SystemUserCredential (Username)
                    var credential = await _credentialRepository.GetByUserIdAsync(user.UserId);
                    if (credential != null)
                    {
                        credential.Username = lecturer.Email;
                        await _credentialRepository.UpdateAsync(credential);
                    }
                }
                
                changed = true;
                // Re-fetch global whitelists for the next step (syncing presence)
                globalWhitelists = await _whitelistRepository.GetByRoleAsync(lecturerRole.RoleId);
            }

            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            
            // Look for an existing global or current semester entry for the CURRENT email
            var existingEntry = globalWhitelists.FirstOrDefault(w => 
                !string.IsNullOrEmpty(w.Email) && 
                w.Email.Trim().Equals(lecturer.Email.Trim(), StringComparison.OrdinalIgnoreCase) && 
                (w.SemesterId == null || w.SemesterId == currentSemester?.SemesterId));

            if (shouldBePresent)
            {
                if (existingEntry == null)
                {
                    await _whitelistRepository.AddAsync(new Whitelist
                    {
                        Email = lecturer.Email,
                        FullName = lecturer.FullName,
                        Avatar = lecturer.Avatar,
                        CampusId = lecturer.CampusId,
                        RoleId = lecturerRole.RoleId,
                        SemesterId = currentSemester?.SemesterId, // Assigned current semester ID
                        AddedDate = DateTime.UtcNow
                    });
                    changed = true;
                }
                else
                {
                    if (existingEntry.FullName != lecturer.FullName || 
                        existingEntry.Avatar != lecturer.Avatar || 
                        existingEntry.CampusId != lecturer.CampusId)
                    {
                        existingEntry.FullName = lecturer.FullName;
                        existingEntry.Avatar = lecturer.Avatar;
                        existingEntry.CampusId = lecturer.CampusId;
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
