using BusinessObjects.Models;

namespace DataAccess
{
    public interface IUserDAO
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
    }
}
