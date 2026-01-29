using BusinessObjects.Models;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class UserDAO : IUserDAO
    {
        private readonly FctmsContext _context;

        public UserDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User> AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Reload to get navigation properties
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            
            // Reload to get navigation properties esp. Role
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
        }

        public async Task<List<User>> SearchUsersAsync(string term)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => (u.FullName.Contains(term) || u.Email.Contains(term) || u.StudentCode.Contains(term)) 
                            && u.Role.RoleName == CampusConstants.Roles.Student) // Only search students
                .Take(10) // Limit results
                .ToListAsync();
        }
    }
}
