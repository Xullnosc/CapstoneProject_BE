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
        Task ArchiveTeamAsync(Team team);
    }

    public class ArchivingRepository : IArchivingRepository
    {
        private readonly FctmsContext _context;
        private readonly ITeamDAO _teamDAO;
        private readonly IWhitelistDAO _whitelistDAO;
        private readonly IArchivedTeamDAO _archivedTeamDAO;
        private readonly IArchivedWhitelistDAO _archivedWhitelistDAO;

        public ArchivingRepository(
            FctmsContext context,
            ITeamDAO teamDAO,
            IWhitelistDAO whitelistDAO,
            IArchivedTeamDAO archivedTeamDAO,
            IArchivedWhitelistDAO archivedWhitelistDAO)
        {
            _context = context;
            _teamDAO = teamDAO;
            _whitelistDAO = whitelistDAO;
            _archivedTeamDAO = archivedTeamDAO;
            _archivedWhitelistDAO = archivedWhitelistDAO;
        }

        public async Task ArchiveSemesterAsync(int semesterId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Archive Whitelists
                var whitelistsToArchive = await _whitelistDAO.GetBySemesterIdAsync(semesterId);

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

                    await _archivedWhitelistDAO.AddRangeAsync(archivedWhitelists);
                    await _whitelistDAO.DeleteRangeAsync(whitelistsToArchive);
                }

                // 2. Archive Teams
                var teamsToArchive = await _teamDAO.GetForArchivingAsync(semesterId);

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

                    await _archivedTeamDAO.AddRangeAsync(archivedTeams);
                    await _teamDAO.DeleteRangeAsync(teamsToArchive);
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ArchiveTeamAsync(Team team)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var archivedTeam = new ArchivedTeam
                {
                    OriginalTeamId = team.TeamId,
                    TeamCode = team.TeamCode,
                    TeamName = team.TeamName,
                    SemesterId = team.SemesterId,
                    LeaderId = team.LeaderId,
                    Status = "Disbanded", // Force status to Disbanded since this action is only for Disband
                    ArchivedAt = DateTime.UtcNow,
                    JsonData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Members = team.Teammembers.Select(m => new { m.StudentId, m.Role }),
                        TopicId = 0 
                    })
                };

                await _archivedTeamDAO.AddAsync(archivedTeam);
                await _teamDAO.DeleteAsync(team);

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
