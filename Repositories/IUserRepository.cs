using BusinessObjects.Models;

namespace Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task<List<User>> SearchUsersAsync(string term);
        Task<List<User>> GetUsersByEmailsAsync(List<string> emails);
        Task<List<User>> GetUsersByIdsAsync(List<int> ids);
        Task<List<User>> GetUsersByRoleAsync(string roleName, string? search);
        Task<bool> HasHodInCampusAsync(int campusId, int? excludeUserId);
        Task<DateTime?> GetLastLoginUtcAsync(int userId);
    }
}
