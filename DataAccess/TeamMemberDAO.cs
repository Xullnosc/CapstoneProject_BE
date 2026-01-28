using BusinessObjects.Models;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess
{
    public class TeamMemberDAO : ITeamMemberDAO
    {
        private readonly FctmsContext _context;

        public TeamMemberDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<Teammember> AddMemberAsync(Teammember member)
        {
            _context.Teammembers.Add(member);
            await _context.SaveChangesAsync();
            return member;
        }

        public async Task<bool> RemoveMemberAsync(int teamId, int studentId)
        {
            var member = await _context.Teammembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.StudentId == studentId);
            
            if (member == null) return false;

            _context.Teammembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Teammember>> GetMembersByTeamIdAsync(int teamId)
        {
            return await _context.Teammembers
                .Include(m => m.Student)
                .Where(m => m.TeamId == teamId)
                .ToListAsync();
        }

        public async Task<Teammember?> GetMemberAsync(int teamId, int studentId)
        {
            return await _context.Teammembers
                .Include(m => m.Student) // Include student info
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.StudentId == studentId);
        }

        public async Task<bool> IsStudentInTeamAsync(int studentId, int semesterId)
        {
            // Check if student is in any team in the given semester
             // Note: We need to join with Team to check SemesterId
            return await _context.Teammembers
                .Include(m => m.Team)
                .AnyAsync(m => m.StudentId == studentId && m.Team.SemesterId == semesterId && m.Team.Status != CampusConstants.TeamStatus.Disbanded);
        }
    }
}
