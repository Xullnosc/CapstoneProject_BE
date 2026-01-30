using BusinessObjects.Models;
using BusinessObjects;
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

        public async Task CancelAllPendingInvitationsForStudentAsync(int studentId)
        {
             var pendingInvitations = await _context.Teaminvitations
                .Where(i => i.StudentId == studentId && i.Status == CampusConstants.InvitationStatus.Pending)
                .ToListAsync();

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
    }
}
