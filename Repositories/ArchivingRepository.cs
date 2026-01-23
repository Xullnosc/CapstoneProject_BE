using BusinessObjects.Models;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public interface IArchivingRepository
    {
        Task ArchiveSemesterAsync(int semesterId);
    }

    public class ArchivingRepository : IArchivingRepository
    {
        private readonly FctmsContext _context;

        public ArchivingRepository(FctmsContext context)
        {
            _context = context;
        }

        public async Task ArchiveSemesterAsync(int semesterId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Archive Whitelists
                var whitelistsToArchive = await _context.Whitelists
                    .Where(w => w.SemesterId == semesterId)
                    .ToListAsync();

                if (whitelistsToArchive.Any())
                {
                    var archivedWhitelists = whitelistsToArchive.Select(w => new ArchivedWhitelist
                    {
                        OriginalWhitelistId = w.WhitelistId,
                        StudentCode = w.StudentCode,
                        Email = w.Email,
                        FullName = w.FullName,
                        RoleId = w.RoleId,
                        Campus = w.Campus,
                        SemesterId = semesterId,
                        ArchivedAt = DateTime.UtcNow
                    });

                    await _context.ArchivedWhitelists.AddRangeAsync(archivedWhitelists);
                    _context.Whitelists.RemoveRange(whitelistsToArchive);
                }

                // 2. Archive Completed/Disbanded Teams
                // Policy: Archive ALL teams of the semester when it ends? 
                // Or only certain statuses? 
                // Assumption: When semester ends, ALL teams should be archived to clear the board.
                var teamsToArchive = await _context.Teams
                    .Where(t => t.SemesterId == semesterId)
                    .Include(t => t.Teammembers)
                    .Include(t => t.Teaminvitations)
                    .ToListAsync();

                if (teamsToArchive.Any())
                {
                    var archivedTeams = teamsToArchive.Select(t => new ArchivedTeam
                    {
                        OriginalTeamId = t.TeamId,
                        TeamCode = t.TeamCode,
                        TeamName = t.TeamName,
                        SemesterId = t.SemesterId,
                        LeaderId = t.LeaderId,
                        Status = t.Status,
                        ArchivedAt = DateTime.UtcNow,
                        // Simple serialization for snapshot
                        JsonData = System.Text.Json.JsonSerializer.Serialize(new { 
                            Members = t.Teammembers.Select(m => new { m.StudentId, m.Role }),
                            TopicId = 0 // Placeholder until Topic implemented
                        })
                    });

                    await _context.ArchivedTeams.AddRangeAsync(archivedTeams);
                    
                    // Delete relationships first due to FKs
                    foreach (var team in teamsToArchive)
                    {
                        _context.Teammembers.RemoveRange(team.Teammembers);
                        _context.Teaminvitations.RemoveRange(team.Teaminvitations);
                    }
                    _context.Teams.RemoveRange(teamsToArchive);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
