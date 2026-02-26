using BusinessObjects.Models;

namespace DataAccess
{
    public interface IUserDAO
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task<List<User>> SearchUsersAsync(string term);
        Task<List<User>> GetUsersByEmailsAsync(List<string> emails);
    }
}
