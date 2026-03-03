using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess
{
    public class TeamInvitationDAO : ITeamInvitationDAO
    {
        private readonly FctmsContext _context;

        public TeamInvitationDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<Teaminvitation> CreateAsync(Teaminvitation invitation)
        {
            _context.Teaminvitations.Add(invitation);
            await _context.SaveChangesAsync();
            return invitation;
        }

        public async Task<Teaminvitation?> GetByIdAsync(int invitationId)
        {
            return await _context.Teaminvitations
                .Include(i => i.Team)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvitationId == invitationId);
        }

        public async Task<List<Teaminvitation>> GetByStudentIdAsync(int studentId)
        {
            return await _context.Teaminvitations
                .Include(i => i.Team)
                    .ThenInclude(t => t.Teammembers)
                .Include(i => i.Team)
                    .ThenInclude(t => t.Leader)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .Where(i => i.StudentId == studentId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Teaminvitation>> GetByStudentIdAsync(int studentId, int pageIndex, int pageSize)
        {
            var query = _context.Teaminvitations
                .Include(i => i.Team)
                    .ThenInclude(t => t.Teammembers)
                .Include(i => i.Team)
                    .ThenInclude(t => t.Leader)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .Where(i => i.StudentId == studentId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Teaminvitation>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<List<Teaminvitation>> GetByTeamIdAsync(int teamId)
        {
            return await _context.Teaminvitations
                .Include(i => i.Team)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .Where(i => i.TeamId == teamId)
                .ToListAsync();
        }

        public async Task<PagedResult<Teaminvitation>> GetByTeamIdAsync(int teamId, int pageIndex, int pageSize)
        {
            var query = _context.Teaminvitations
                .Include(i => i.Team)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .Where(i => i.TeamId == teamId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Teaminvitation>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<bool> UpdateStatusAsync(int invitationId, string status)
        {
            var invitation = await _context.Teaminvitations.FindAsync(invitationId);
            if (invitation == null) return false;

            invitation.Status = status;
            if (status == CampusConstants.InvitationStatus.Accepted || status == CampusConstants.InvitationStatus.Declined)
            {
                invitation.RespondedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int invitationId)
        {
            var invitation = await _context.Teaminvitations.FindAsync(invitationId);
            if (invitation == null) return false;

            _context.Teaminvitations.Remove(invitation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Teaminvitation>> GetPendingInvitationsByStudentAsync(int studentId)
        {
            return await _context.Teaminvitations
                .Include(i => i.Team)
                    .ThenInclude(t => t.Teammembers)
                .Include(i => i.Team)
                    .ThenInclude(t => t.Leader)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                 .AsNoTracking()
                 .Where(i => i.StudentId == studentId && i.Status == CampusConstants.InvitationStatus.Pending)
                 .OrderByDescending(i => i.CreatedAt)
                 .ToListAsync();
        }

        public async Task<PagedResult<Teaminvitation>> GetPendingInvitationsByStudentAsync(int studentId, int pageIndex, int pageSize)
        {
            var query = _context.Teaminvitations
                .Include(i => i.Team)
                    .ThenInclude(t => t.Teammembers)
                .Include(i => i.Team)
                    .ThenInclude(t => t.Leader)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .Where(i => i.StudentId == studentId && i.Status == CampusConstants.InvitationStatus.Pending);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Teaminvitation>(items, totalCount, pageIndex, pageSize);
        }

        public async Task CancelAllPendingInvitationsForStudentAsync(int studentId)
        {
             var pendingInvitations = await _context.Teaminvitations
                .Where(i => i.StudentId == studentId && i.Status == CampusConstants.InvitationStatus.Pending)
                .ToListAsync(); // No AsNoTracking — needs tracking to update

            if (pendingInvitations.Any())
            {
                foreach (var inv in pendingInvitations)
                {
                    inv.Status = CampusConstants.InvitationStatus.Cancelled;
                    inv.RespondedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Teaminvitation?> GetByTeamAndStudentAsync(int teamId, int studentId)
        {
            return await _context.Teaminvitations
                .Include(i => i.Team)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.TeamId == teamId && i.StudentId == studentId && i.Status == CampusConstants.InvitationStatus.Pending);
        }

        // --- Mentor Invitation Methods ---

        public async Task<List<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId)
        {
            return await _context.Teaminvitations
                .Include(i => i.Team)
                    .ThenInclude(t => t.Teammembers)
                .Include(i => i.Team)
                    .ThenInclude(t => t.Leader)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .Where(i => i.StudentId == mentorId 
                         && i.Type == CampusConstants.InvitationType.Mentor 
                         && i.Status == CampusConstants.InvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Teaminvitation>> GetPendingMentorInvitationsByMentorIdAsync(int mentorId, int pageIndex, int pageSize)
        {
            var query = _context.Teaminvitations
                .Include(i => i.Team)
                    .ThenInclude(t => t.Teammembers)
                .Include(i => i.Team)
                    .ThenInclude(t => t.Leader)
                .Include(i => i.InvitedByNavigation)
                .Include(i => i.Student)
                .AsNoTracking()
                .Where(i => i.StudentId == mentorId 
                         && i.Type == CampusConstants.InvitationType.Mentor 
                         && i.Status == CampusConstants.InvitationStatus.Pending);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Teaminvitation>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<List<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId)
        {
            return await _context.Teaminvitations
                .Include(i => i.Student)
                .Include(i => i.InvitedByNavigation)
                .AsNoTracking()
                .Where(i => i.TeamId == teamId && i.Type == CampusConstants.InvitationType.Mentor)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Teaminvitation>> GetMentorInvitationsByTeamAsync(int teamId, int pageIndex, int pageSize)
        {
            var query = _context.Teaminvitations
                .Include(i => i.Student)
                .Include(i => i.InvitedByNavigation)
                .AsNoTracking()
                .Where(i => i.TeamId == teamId && i.Type == CampusConstants.InvitationType.Mentor);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Teaminvitation>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<Teaminvitation?> GetByTeamAndMentorAsync(int teamId, int mentorId)
        {
            return await _context.Teaminvitations
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.TeamId == teamId 
                                       && i.StudentId == mentorId 
                                       && i.Type == CampusConstants.InvitationType.Mentor 
                                       && i.Status == CampusConstants.InvitationStatus.Pending);
        }

        public async Task<int> GetMentorActiveTeamCountAsync(int mentorId, int semesterId)
        {
            return await _context.Teams
                .AsNoTracking()
                .CountAsync(t => t.MentorId == mentorId 
                              && t.SemesterId == semesterId 
                              && t.Status != CampusConstants.TeamStatus.Disbanded);
        }
    }
}
