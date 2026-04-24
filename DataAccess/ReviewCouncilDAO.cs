using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class ReviewCouncilDAO : IReviewCouncilDAO
    {
        private readonly FctmsContext _context;

        public ReviewCouncilDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewCouncil>> GetCouncilsBySemesterAsync(int semesterId)
        {
            return await _context.ReviewCouncils
                .Include(c => c.ReviewCouncilMembers).ThenInclude(m => m.Lecturer)
                .Include(c => c.ReviewCouncilTeams).ThenInclude(t => t.Team)
                .AsNoTracking()
                .Where(c => c.SemesterId == semesterId)
                .ToListAsync();
        }

        public async Task<ReviewCouncil?> GetCouncilByIdAsync(int councilId)
        {
            return await _context.ReviewCouncils
                .Include(c => c.ReviewCouncilMembers).ThenInclude(m => m.Lecturer)
                .Include(c => c.ReviewCouncilTeams).ThenInclude(t => t.Team)
                .FirstOrDefaultAsync(c => c.Id == councilId);
        }

        public async Task AddCouncilAsync(ReviewCouncil council)
        {
            await _context.ReviewCouncils.AddAsync(council);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCouncilAsync(ReviewCouncil council)
        {
            _context.ReviewCouncils.Update(council);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCouncilAsync(int councilId)
        {
            var council = await _context.ReviewCouncils.FindAsync(councilId);
            if (council != null)
            {
                _context.ReviewCouncils.Remove(council);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddMemberAsync(ReviewCouncilMember member)
        {
            await _context.ReviewCouncilMembers.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(int councilId, int lecturerId)
        {
            var member = await _context.ReviewCouncilMembers.FirstOrDefaultAsync(m => m.CouncilId == councilId && m.LecturerId == lecturerId);
            if (member != null)
            {
                _context.ReviewCouncilMembers.Remove(member);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddTeamAsync(ReviewCouncilTeam team)
        {
            await _context.ReviewCouncilTeams.AddAsync(team);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveTeamAsync(int councilId, int teamId)
        {
            var team = await _context.ReviewCouncilTeams.FirstOrDefaultAsync(t => t.CouncilId == councilId && t.TeamId == teamId);
            if (team != null)
            {
                _context.ReviewCouncilTeams.Remove(team);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ReviewCouncilTeam?> GetCouncilTeamAsync(int councilId, int teamId)
        {
            return await _context.ReviewCouncilTeams
                .FirstOrDefaultAsync(ct => ct.CouncilId == councilId && ct.TeamId == teamId);
        }

        public async Task UpdateCouncilTeamAsync(ReviewCouncilTeam councilTeam)
        {
            _context.ReviewCouncilTeams.Update(councilTeam);
            await _context.SaveChangesAsync();
        }
    }
}
