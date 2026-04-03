using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
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
                .IgnoreQueryFilters()
                .Include(u => u.Role)
                .Include(u => u.AccountDetail)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.AccountDetail)
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

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
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

        public async Task<PagedResult<User>> SearchUsersAsync(string term, int pageIndex, int pageSize)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Where(u => (u.FullName.Contains(term) || u.Email.Contains(term) || u.StudentCode.Contains(term))
                            && u.Role.RoleName == CampusConstants.Roles.Student);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.UserId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<User>(items, totalCount, pageIndex, pageSize);
        }
        public async Task<List<User>> GetUsersByEmailsAsync(List<string> emails)
        {
            if (emails == null || !emails.Any()) return new List<User>();

            var lowerEmails = emails.Select(e => e.ToLower().Trim()).ToList();

            return await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Role)
                .Where(u => u.Email != null && lowerEmails.Contains(u.Email.ToLower()))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PagedResult<User>> GetUsersByEmailsAsync(List<string> emails, int pageIndex, int pageSize)
        {
            if (emails == null || !emails.Any())
                return new PagedResult<User>(new List<User>(), 0, pageIndex, pageSize);

            var lowerEmails = emails.Select(e => e.ToLower().Trim()).ToList();

            var query = _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Role)
                .Where(u => lowerEmails.Contains(u.Email.ToLower()))
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.UserId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<User>(items, totalCount, pageIndex, pageSize);
        }
    }
}
