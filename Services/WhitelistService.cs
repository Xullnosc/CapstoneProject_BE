using BusinessObjects.Models;
using Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class WhitelistService : IWhitelistService
    {
        private readonly IWhitelistRepository _whitelistRepository;

        public WhitelistService(IWhitelistRepository whitelistRepository)
        {
            _whitelistRepository = whitelistRepository;
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
        }
    }
}
