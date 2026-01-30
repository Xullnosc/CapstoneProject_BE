using BusinessObjects.Models;

namespace Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task<List<User>> SearchUsersAsync(string term);
    }
}
