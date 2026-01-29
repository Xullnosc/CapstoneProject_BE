using BusinessObjects.Models;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<List<User>> SearchUsersAsync(string term)
        {
            return await _userDAO.SearchUsersAsync(term);
        }
    }
}
