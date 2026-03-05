using BusinessObjects.Models;
using Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class WhitelistService : IWhitelistService
    {
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly IRedisService _redisService;

        public WhitelistService(IWhitelistRepository whitelistRepository, IRedisService redisService)
        {
            _whitelistRepository = whitelistRepository;
            _redisService = redisService;
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

            whitelist.IsReviewer = isReviewer;
            await _whitelistRepository.UpdateAsync(whitelist);

            await InvalidateCache(whitelist.SemesterId);
        }

        public async Task<Whitelist> AddStudentToWhitelistAsync(Whitelist whitelist)
        {
            await _whitelistRepository.AddAsync(whitelist);
            await InvalidateCache(whitelist.SemesterId);
            return whitelist;
        }

        public async Task UpdateWhitelistAsync(Whitelist whitelist)
        {
            var existing = await _whitelistRepository.GetByIdAsync(whitelist.WhitelistId);
            if (existing == null) return;

            // Update properties
            existing.Email = whitelist.Email;
            existing.FullName = whitelist.FullName;
            existing.Campus = whitelist.Campus;
            existing.StudentCode = whitelist.StudentCode;
            existing.RoleId = whitelist.RoleId;
            existing.IsReviewer = whitelist.IsReviewer;
            existing.Avatar = whitelist.Avatar;

            await _whitelistRepository.UpdateAsync(existing);
            await InvalidateCache(existing.SemesterId);
        }

        public async Task DeleteWhitelistAsync(int id)
        {
            var whitelist = await _whitelistRepository.GetByIdAsync(id);
            if (whitelist != null)
            {
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
    }
}
