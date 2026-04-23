using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class DiscoveryDAO : IDiscoveryDAO
    {
        private readonly FctmsContext _context;

        public DiscoveryDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<(List<User> Items, int TotalCount)> GetLookingStudentsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize)
        {
            // 1. Get base query from Whitelists (All eligible students)
            var baseQuery = _context.Whitelists
                .Where(w => w.SemesterId == semesterId 
                            && w.CampusId == campusId 
                            && (w.RoleId == 3 || (w.Role != null && w.Role.RoleName == BusinessObjects.CampusConstants.Roles.Student))
                            && w.Status == BusinessObjects.CampusConstants.WhitelistStatus.Qualified);

            // 2. Filter out current user and those already in a team in this semester
            var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == currentUserId);
            var currentUserEmail = currentUser?.Email?.ToLower() ?? "";

            var studentsInTeams = _context.Teammembers
                .Where(tm => tm.Team.SemesterId == semesterId)
                .Select(tm => tm.Student.Email);

            var leadersInTeams = _context.Teams
                .Where(t => t.SemesterId == semesterId)
                .Select(t => t.Leader.Email);

            var query = baseQuery.Where(w => 
                w.Email.ToLower() != currentUserEmail && 
                !studentsInTeams.Contains(w.Email) && 
                !leadersInTeams.Contains(w.Email));

            // 3. Optional Search Query (Database-side search on Name/Code)
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var lowerSearch = searchQuery.ToLower();
                query = query.Where(w => w.FullName.ToLower().Contains(lowerSearch) || 
                                         (w.StudentCode != null && w.StudentCode.ToLower().Contains(lowerSearch)));
            }

            // 4. Optional Skill Filtering (Fuzzy Search)
            if (!string.IsNullOrEmpty(skillFilter))
            {
                var lowerSkill = skillFilter.ToLower();
                query = query.Where(w => _context.UserSkills.Any(s => s.User.Email == w.Email && s.SkillTag.ToLower().Contains(lowerSkill)));
            }

            var total = await query.CountAsync();

            // 5. Project results into User objects
            var whitelistItems = await query
                .OrderBy(w => w.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var emails = whitelistItems.Select(w => w.Email).ToList();
            var existingUsers = await _context.Users
                .Include(u => u.AccountDetail)
                .Include(u => u.UserSkills)
                .Where(u => emails.Contains(u.Email))
                .ToListAsync();

            var items = new List<User>();
            foreach (var w in whitelistItems)
            {
                var user = existingUsers.FirstOrDefault(u => u.Email == w.Email);
                if (user != null)
                {
                    items.Add(user);
                }
                else
                {
                    // Create unpersisted stub with negative ID for whitelist-only students
                    items.Add(new User
                    {
                        UserId = -w.WhitelistId,
                        Email = w.Email,
                        FullName = w.FullName,
                        StudentCode = w.StudentCode,
                        CampusId = w.CampusId,
                        RoleId = w.RoleId,
                        Avatar = $"https://ui-avatars.com/api/?name={System.Net.WebUtility.UrlEncode(w.FullName)}&background=random&color=fff"
                    });
                }
            }

            return (items, total);
        }

        public async Task<(List<Team> Items, int TotalCount)> GetOpenTeamsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize)
        {
            var query = _context.Teams
                .Include(t => t.Teammembers)
                    .ThenInclude(tm => tm.Student)
                        .ThenInclude(s => s.UserSkills)
                .Include(t => t.Teaminvitations)
                .Where(t => t.SemesterId == semesterId 
                            && t.CampusId == campusId 
                            && t.Teammembers.Count < 5
                            && !t.Teammembers.Any(tm => tm.StudentId == currentUserId)); // Filter out own team

            // Optional Search Query (Database-side search on TeamName/Code)
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var lowerSearch = searchQuery.ToLower();
                query = query.Where(t => t.TeamName.ToLower().Contains(lowerSearch) || 
                                         (t.TeamCode != null && t.TeamCode.ToLower().Contains(lowerSearch)));
            }

            if (!string.IsNullOrEmpty(skillFilter))
            {
                var lowerSkill = skillFilter.ToLower();
                query = query.Where(t => t.Teammembers.Any(tm => tm.Student.UserSkills.Any(s => s.SkillTag.ToLower().Contains(lowerSkill))));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<List<string>> GetTopSkillsAsync(int count)
        {
            return await _context.UserSkills
                .GroupBy(s => s.SkillTag.ToLower())
                .OrderByDescending(g => g.Count())
                .Select(g => g.First().SkillTag) 
                .Take(count)
                .ToListAsync();
        }

        public async Task UpdateUserSkillsAsync(int userId, List<UserSkill> skills)
        {
            var existing = await _context.UserSkills.Where(s => s.UserId == userId).ToListAsync();
            _context.UserSkills.RemoveRange(existing);
            
            if (skills.Any())
            {
                await _context.UserSkills.AddRangeAsync(skills);
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsUserInTeamAsync(int userId, int semesterId)
        {
            return await _context.Teammembers.AnyAsync(tm => tm.Team.SemesterId == semesterId && tm.StudentId == userId)
                || await _context.Teams.AnyAsync(t => t.SemesterId == semesterId && t.LeaderId == userId);
        }

        public async Task<List<UserSkill>> GetUserSkillsAsync(int userId)
        {
            return await _context.UserSkills
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }
    }
}
