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

            // Invalidate semester cache so the next fetch returns fresh data
            await _redisService.DeleteValueAsync("fctms:semester:all");
            if (whitelist.SemesterId.HasValue)
            {
                await _redisService.DeleteValueAsync($"fctms:semester:id:{whitelist.SemesterId.Value}");
            }
        }
    }
}
