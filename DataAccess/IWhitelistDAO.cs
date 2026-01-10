using BusinessObjects.Models;

namespace DataAccess
{
    public interface IWhitelistDAO
    {
        Task<Whitelist?> GetByEmailAsync(string email);
    }
}
