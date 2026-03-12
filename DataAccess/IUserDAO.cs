using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace DataAccess
{
    public interface IUserDAO
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task<List<User>> SearchUsersAsync(string term);
        Task<PagedResult<User>> SearchUsersAsync(string term, int pageIndex, int pageSize);
        Task<List<User>> GetUsersByEmailsAsync(List<string> emails);
        Task<PagedResult<User>> GetUsersByEmailsAsync(List<string> emails, int pageIndex, int pageSize);
    }
}
