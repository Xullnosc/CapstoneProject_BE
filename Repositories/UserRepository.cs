using BusinessObjects.Models;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IUserDAO _userDAO;

        public UserRepository(IUserDAO userDAO)
        {
            _userDAO = userDAO;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _userDAO.GetByEmailAsync(email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _userDAO.GetByIdAsync(id);
        }

        public async Task<User> AddAsync(User user)
        {
            return await _userDAO.AddAsync(user);
        }

        public async Task UpdateAsync(User user)
        {
            await _userDAO.UpdateAsync(user);
        }

        public async Task DeleteAsync(User user)
        {
            await _userDAO.DeleteAsync(user);
        }

        public async Task<List<User>> SearchUsersAsync(string term)
        {
            return await _userDAO.SearchUsersAsync(term);
        }

        public async Task<List<User>> GetUsersByEmailsAsync(List<string> emails)
        {
            return await _userDAO.GetUsersByEmailsAsync(emails);
        }

        public async Task<List<User>> GetUsersByIdsAsync(List<int> ids) => await _userDAO.GetUsersByIdsAsync(ids);
        public async Task<List<User>> GetUsersByRoleAsync(string roleName, string? search) => await _userDAO.GetUsersByRoleAsync(roleName, search);
        public async Task<bool> HasHodInCampusAsync(int campusId, int? excludeUserId) => await _userDAO.HasHodInCampusAsync(campusId, excludeUserId);
        public async Task<DateTime?> GetLastLoginUtcAsync(int userId) => await _userDAO.GetLastLoginUtcAsync(userId);
    }
}
