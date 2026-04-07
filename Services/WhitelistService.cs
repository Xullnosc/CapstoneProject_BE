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
            return await _whitelistRepository.GetByRoleAsync(roleId);
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
            var existing = await _whitelistRepository.GetByEmailAsync(whitelist.Email.Trim());

            if (existing != null)
            {
                var previousSemesterId = existing.SemesterId;

                existing.Email = whitelist.Email.Trim();
                existing.FullName = whitelist.FullName;
                existing.StudentCode = whitelist.StudentCode;
                existing.RoleId = whitelist.RoleId;
                existing.Avatar = whitelist.Avatar;
                existing.Status = whitelist.Status ?? (existing.RoleId == 3 ? "Qualified" : null);
                existing.CampusId = semester.CampusId;
                existing.SemesterId = semester.SemesterId;

                await _whitelistRepository.UpdateAsync(existing);

                if (previousSemesterId != existing.SemesterId)
                {
                    await InvalidateCache(previousSemesterId);
                }

                await InvalidateCache(existing.SemesterId);
                return existing;
            }

            whitelist.Email = whitelist.Email.Trim();
            whitelist.CampusId = semester.CampusId;
            if (whitelist.RoleId == 3 && string.IsNullOrWhiteSpace(whitelist.Status))
            {
                whitelist.Status = "Qualified";
            }

            await _whitelistRepository.AddAsync(whitelist);
            await InvalidateCache(whitelist.SemesterId);
            return whitelist;
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
            await InvalidateCache(existing.SemesterId);
        }

        public async Task DeleteWhitelistAsync(int id)
        {
            var whitelist = await _whitelistRepository.GetByIdAsync(id);
            if (whitelist != null)
            {
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
