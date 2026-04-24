using BusinessObjects.Models;
using Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class WhitelistService : IWhitelistService
    {
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IRedisService _redisService;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISystemUserCredentialRepository _credentialRepository;

        public WhitelistService(
            IWhitelistRepository whitelistRepository, 
            ISemesterRepository semesterRepository,
            IRedisService redisService, 
            ILecturerRepository lecturerRepository,
            IUserRepository userRepository,
            ISystemUserCredentialRepository credentialRepository)
        {
            _whitelistRepository = whitelistRepository;
            _semesterRepository = semesterRepository;
            _redisService = redisService;
            _lecturerRepository = lecturerRepository;
            _userRepository = userRepository;
            _credentialRepository = credentialRepository;
        }

        public async Task<IEnumerable<Whitelist>> GetWhitelistByRoleAsync(int roleId)
        {
            var whitelists = await _whitelistRepository.GetByRoleAsync(roleId);
            var whitelistList = whitelists.ToList();

            if (!whitelistList.Any()) return whitelistList;

            var emails = whitelistList.Select(w => w.Email?.Trim().ToLowerInvariant()).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
            var users = await _userRepository.GetUsersByEmailsAsync(emails!);
            var avatarDict = users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .GroupBy(u => u.Email!.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First().Avatar);

            foreach (var w in whitelistList)
            {
                if (!string.IsNullOrEmpty(w.Email))
                {
                    string emailKey = w.Email.Trim().ToLowerInvariant();
                    bool hasNoAvatar = string.IsNullOrWhiteSpace(w.Avatar) || w.Avatar == "N/A";

                    if (hasNoAvatar && avatarDict.TryGetValue(emailKey, out var userAvatar) && !string.IsNullOrWhiteSpace(userAvatar))
                    {
                        w.Avatar = userAvatar;
                    }
                }
            }

            return whitelistList;
        }

        public async Task UpdateReviewerStatusAsync(int whitelistId, bool isReviewer)
        {
            var whitelist = await _whitelistRepository.GetByIdAsync(whitelistId);
            if (whitelist == null)
            {
                throw new KeyNotFoundException($"Whitelist entry with ID {whitelistId} not found.");
            }

            // Move the IsReviewer status logic to the Lecturer table
            var lecturer = await _lecturerRepository.GetByEmailAsync(whitelist.Email);
            if (lecturer != null)
            {
                lecturer.IsReviewer = isReviewer;
                await _lecturerRepository.UpdateAsync(lecturer);
            }
            else if (isReviewer)
            {
                // If they don't exist in Lecturer yet but are being assigned as reviewer, 
                // we should create a basic lecturer entry for them.
                var newLecturer = new Lecturer
                {
                    Email = whitelist.Email,
                    FullName = whitelist.FullName ?? "New Lecturer",
                    CampusId = whitelist.CampusId,
                    IsReviewer = true,
                    IsActive = true
                };
                await _lecturerRepository.AddAsync(newLecturer);
            }

            await InvalidateCache(whitelist.SemesterId);
        }

        public async Task<Whitelist> AddStudentToWhitelistAsync(Whitelist whitelist)
        {
            var semester = await GetRequiredSemesterAsync(whitelist.SemesterId);
            var normalizedEmail = whitelist.Email.Trim();
            
            // 1. Check if an entry already exists specifically in the target semester
            var existingInSemester = await _whitelistRepository.GetByEmailAndSemesterAsync(normalizedEmail, semester.SemesterId);

            if (existingInSemester != null)
            {
                // Update properties of the existing entry in the CURRENT semester
                existingInSemester.Email = normalizedEmail;
                existingInSemester.FullName = whitelist.FullName;
                existingInSemester.StudentCode = whitelist.StudentCode;
                existingInSemester.RoleId = whitelist.RoleId;
                existingInSemester.Avatar = whitelist.Avatar;
                
                // If status is provided, use it; otherwise, if it's a student (Role 3), default to Qualified
                existingInSemester.Status = whitelist.Status ?? (existingInSemester.RoleId == 3 ? "Qualified" : existingInSemester.Status);
                existingInSemester.CampusId = semester.CampusId;

                await _whitelistRepository.UpdateAsync(existingInSemester);
                await InvalidateCache(existingInSemester.SemesterId);
                return existingInSemester;
            }

            // 2. Not found in specific semester -> Create a NEW entry
            // But first, check if they exist in ANY previous semester to determine qualification/basic info
            var historicalEntry = await _whitelistRepository.GetByEmailAsync(normalizedEmail);
            
            var newEntry = new Whitelist
            {
                Email = normalizedEmail,
                FullName = whitelist.FullName ?? historicalEntry?.FullName,
                StudentCode = whitelist.StudentCode ?? historicalEntry?.StudentCode,
                RoleId = whitelist.RoleId,
                Avatar = whitelist.Avatar ?? historicalEntry?.Avatar,
                CampusId = semester.CampusId,
                SemesterId = semester.SemesterId,
            };

            // Status logic: if provided, use it. 
            // If not provided:
            // - If Role is Student (3) AND (they were in a previous whitelist OR we are adding them now), mark as Qualified (matching import logic)
            if (!string.IsNullOrWhiteSpace(whitelist.Status))
            {
                newEntry.Status = whitelist.Status;
            }
            else if (newEntry.RoleId == 3)
            {
                newEntry.Status = "Qualified";
            }

            await _whitelistRepository.AddAsync(newEntry);
            
            // Sync user account immediately so they can be invited to teams/receive notifications
            await SyncUserFromWhitelistAsync(newEntry);

            await InvalidateCache(newEntry.SemesterId);
            return newEntry;
        }

        public async Task UpdateWhitelistAsync(Whitelist whitelist)
        {
            var existing = await _whitelistRepository.GetByIdAsync(whitelist.WhitelistId);
            if (existing == null) return;

            var targetSemesterId = whitelist.SemesterId ?? existing.SemesterId;
            var semester = await GetRequiredSemesterAsync(targetSemesterId);

            // Update properties
            existing.Email = whitelist.Email;
            existing.FullName = whitelist.FullName;
            existing.CampusId = semester.CampusId;
            existing.StudentCode = whitelist.StudentCode;
            existing.RoleId = whitelist.RoleId;
            // existing.IsReviewer = whitelist.IsReviewer; // Property removed from models
            existing.Avatar = whitelist.Avatar;
            existing.Status = whitelist.Status;
            existing.SemesterId = semester.SemesterId;

            await _whitelistRepository.UpdateAsync(existing);

            // Sync user account to keep name/code/status in sync
            await SyncUserFromWhitelistAsync(existing);

            await InvalidateCache(existing.SemesterId);
        }

        private async Task SyncUserFromWhitelistAsync(Whitelist whitelist)
        {
            if (whitelist == null || string.IsNullOrWhiteSpace(whitelist.Email)) return;

            var email = whitelist.Email.Trim().ToLower();
            var user = await _userRepository.GetByEmailAsync(email);

            bool isQualified = string.Equals(whitelist.Status, "Qualified", StringComparison.OrdinalIgnoreCase);

            if (user == null)
            {
                user = new User
                {
                    Email = whitelist.Email,
                    FullName = whitelist.FullName,
                    StudentCode = whitelist.StudentCode,
                    RoleId = whitelist.RoleId,
                    CampusId = whitelist.CampusId,
                    IsAuthorized = isQualified,
                    CreatedAt = DateTime.UtcNow,
                    Avatar = whitelist.Avatar ?? "N/A"
                };
                await _userRepository.AddAsync(user);
            }
            else
            {
                // Update basic info and authorization status
                user.FullName = whitelist.FullName ?? user.FullName;
                user.StudentCode = whitelist.StudentCode ?? user.StudentCode;
                
                // Only update role if it's currently a student or being set to something higher
                // (Prevents downgrading HOD/Admin to Student via whitelist if they use same email)
                if (user.RoleId == 3 || (whitelist.RoleId.HasValue && whitelist.RoleId != 3))
                {
                    user.RoleId = whitelist.RoleId ?? user.RoleId;
                }

                user.CampusId = whitelist.CampusId;
                user.IsAuthorized = isQualified;
                
                await _userRepository.UpdateAsync(user);
            }
        }

        public async Task DeleteWhitelistAsync(int id)
        {
            var whitelist = await _whitelistRepository.GetByIdAsync(id);
            if (whitelist != null)
            {
                // Deactivate the lecturer in the global pool if they exist
                if (!string.IsNullOrEmpty(whitelist.Email))
                {
                    var lecturer = await _lecturerRepository.GetByEmailAsync(whitelist.Email);
                    if (lecturer != null && lecturer.IsActive)
                    {
                        lecturer.IsActive = false;
                        await _lecturerRepository.UpdateAsync(lecturer);
                    }
                }

                // Delete user's credentials if they exist
                var user = await _userRepository.GetByEmailAsync(whitelist.Email);
                if (user != null)
                {
                    var credential = await _credentialRepository.GetByUserIdAsync(user.UserId);
                    if (credential != null)
                    {
                        await _credentialRepository.DeleteAsync(credential);
                    }
                }

                await _whitelistRepository.DeleteAsync(whitelist);
                await InvalidateCache(whitelist.SemesterId);
            }
        }

        private async Task InvalidateCache(int? semesterId)
        {
            if (semesterId.HasValue)
            {
                await _redisService.DeleteValueAsync("fctms:semester:all");
                await _redisService.DeleteValueAsync($"fctms:semester:id:{semesterId.Value}");
            }
            else
            {
                // If semesterId is null, it's a global whitelist (like a Lecturer)
                // We must invalidate all cached semesters because they all include global lecturers
                await _redisService.RemoveByPrefixAsync("fctms:semester:");
            }
        }

        private async Task<Semester> GetRequiredSemesterAsync(int? semesterId)
        {
            if (!semesterId.HasValue)
            {
                throw new KeyNotFoundException("SemesterId is required for student whitelist entries.");
            }

            var semester = await _semesterRepository.GetSemesterByIdAsync(semesterId.Value);
            if (semester == null)
            {
                throw new KeyNotFoundException($"Semester with ID {semesterId.Value} not found.");
            }

            return semester;
        }
    }
}
