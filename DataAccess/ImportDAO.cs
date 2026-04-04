using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess
{
    public class ImportDAO : IImportDAO
    {
        private readonly FctmsContext _context;

        public ImportDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<string?> GetUserCampusByEmailAsync(string normalizedEmail)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            return user?.Campus;
        }

        public async Task<List<User>> GetUsersForConflictCheckAsync(List<string> normalizedEmails, List<string> normalizedStudentCodes)
        {
            return await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .Where(u => normalizedEmails.Contains(u.Email.ToLower()) ||
                            (u.StudentCode != null && normalizedStudentCodes.Contains(u.StudentCode.ToLower())))
                .ToListAsync();
        }

        public async Task<List<Whitelist>> GetWhitelistsForConflictCheckAsync(List<string> normalizedEmails, List<string> normalizedStudentCodes)
        {
            return await _context.Whitelists
                .Include(w => w.Role)
                .AsNoTracking()
                .Where(w => normalizedEmails.Contains(w.Email.ToLower()) ||
                            (w.StudentCode != null && normalizedStudentCodes.Contains(w.StudentCode.ToLower())))
                .ToListAsync();
        }

        public async Task ReconcileSemesterAsync(int semesterId, List<WhitelistImportDTO> importedItems, int studentRoleId, DateTime now)
        {
            IDbContextTransaction? transaction = null;
            var providerName = _context.Database.ProviderName ?? string.Empty;
            var isInMemoryProvider = providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase);

            if (!isInMemoryProvider)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                var importedEmails = importedItems
                    .Select(i => NormalizeEmail(i.Email))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .ToList();

                var importedStudentCodes = importedItems
                    .Select(i => NormalizeKey(i.StudentCode))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .ToList();

                var existingWhitelistsInSemester = await _context.Whitelists
                    .Where(w => w.SemesterId == semesterId && w.RoleId == studentRoleId)
                    .ToListAsync();

                var existingMatchingWhitelists = await _context.Whitelists
                    .Where(w => w.RoleId == studentRoleId &&
                                (importedEmails.Contains(w.Email.ToLower()) ||
                                 (w.StudentCode != null && importedStudentCodes.Contains(w.StudentCode.ToLower()))))
                    .ToListAsync();

                var existingWhitelists = existingWhitelistsInSemester
                    .Concat(existingMatchingWhitelists)
                    .GroupBy(w => w.WhitelistId)
                    .Select(group => group.First())
                    .ToList();

                var candidateEmails = importedEmails
                    .Concat(existingWhitelistsInSemester.Select(w => NormalizeEmail(w.Email)))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .ToList();

                var candidateStudentCodes = importedStudentCodes
                    .Concat(existingWhitelistsInSemester.Select(w => NormalizeKey(w.StudentCode)))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .ToList();

                var existingUsers = await _context.Users
                    .Where(u => u.RoleId == studentRoleId &&
                                (candidateEmails.Contains(u.Email.ToLower()) ||
                                 (u.StudentCode != null && candidateStudentCodes.Contains(u.StudentCode.ToLower()))))
                    .ToListAsync();

                var matchedWhitelistIds = new HashSet<int>();
                var matchedUserIds = new HashSet<int>();
                var processedEmails = new HashSet<string>();
                var processedStudentCodes = new HashSet<string>();

                foreach (var importedItem in importedItems)
                {
                    var normalizedEmail = NormalizeEmail(importedItem.Email);
                    var normalizedStudentCode = NormalizeKey(importedItem.StudentCode);

                    // Defensive guard: skip duplicate keys within the same import batch.
                    // The service layer should already validate this and surface errors,
                    // but this protects against unexpected duplicate payloads.
                    if (!string.IsNullOrWhiteSpace(normalizedEmail) && !processedEmails.Add(normalizedEmail))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(normalizedStudentCode) && !processedStudentCodes.Add(normalizedStudentCode))
                    {
                        continue;
                    }

                    var whitelistMatch = FindMatchingWhitelist(importedItem, existingWhitelists, matchedWhitelistIds);
                    if (whitelistMatch == null)
                    {
                        whitelistMatch = new Whitelist();
                        _context.Whitelists.Add(whitelistMatch);
                    }

                    whitelistMatch.Email = importedItem.Email;
                    whitelistMatch.StudentCode = importedItem.StudentCode;
                    whitelistMatch.FullName = importedItem.FullName;
                    whitelistMatch.RoleId = studentRoleId;
                    var campusRef = _context.Campuses.Local.FirstOrDefault(c => c.CampusId == importedItem.CampusId) 
                                    ?? await _context.Campuses.FindAsync(importedItem.CampusId);
                    string campusName = campusRef?.CampusName ?? "";

                    whitelistMatch.CampusId = importedItem.CampusId.Value;
                    whitelistMatch.Campus = campusName;
                    whitelistMatch.SemesterId = semesterId;
                    whitelistMatch.AddedDate = whitelistMatch.AddedDate ?? now;

                    if (whitelistMatch.WhitelistId != 0)
                    {
                        matchedWhitelistIds.Add(whitelistMatch.WhitelistId);
                    }

                    var userMatch = FindMatchingUser(importedItem, existingUsers, matchedUserIds);
                    if (userMatch == null)
                    {
                        userMatch = new User
                        {
                            CreatedAt = now,
                        };
                        _context.Users.Add(userMatch);
                    }

                    userMatch.Email = importedItem.Email;
                    userMatch.StudentCode = importedItem.StudentCode;
                    userMatch.FullName = importedItem.FullName;
                    userMatch.CampusId = importedItem.CampusId.Value;
                    userMatch.Campus = campusName;
                    userMatch.RoleId = studentRoleId;
                    userMatch.IsAuthorized = true;

                    if (userMatch.UserId != 0)
                    {
                        matchedUserIds.Add(userMatch.UserId);
                    }
                }

                var usersToDelete = existingUsers.Where(u => !matchedUserIds.Contains(u.UserId)).ToList();
                foreach (var userToDelete in usersToDelete)
                {
                    await DeleteStudentUserAsync(userToDelete, studentRoleId);
                }

                var whitelistsToDelete = existingWhitelistsInSemester.Where(w => !matchedWhitelistIds.Contains(w.WhitelistId)).ToList();
                foreach (var whitelistToDelete in whitelistsToDelete)
                {
                    bool alreadyRemovedWithUser = usersToDelete.Any(user =>
                        NormalizeEmail(user.Email) == NormalizeEmail(whitelistToDelete.Email) ||
                        NormalizeKey(user.StudentCode) == NormalizeKey(whitelistToDelete.StudentCode));

                    if (!alreadyRemovedWithUser)
                    {
                        _context.Whitelists.Remove(whitelistToDelete);
                    }
                }

                await _context.SaveChangesAsync();
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private async Task DeleteStudentUserAsync(User user, int studentRoleId)
        {
            var relatedTeams = await _context.Teams
                .Include(t => t.Teammembers)
                .Where(t => t.LeaderId == user.UserId || t.Teammembers.Any(m => m.StudentId == user.UserId))
                .ToListAsync();

            foreach (var team in relatedTeams)
            {
                int? reassignedLeaderId = team.LeaderId == user.UserId ? null : team.LeaderId;

                bool teamRemoved = false;
                if (team.LeaderId == user.UserId)
                {
                    var replacementLeader = team.Teammembers
                        .Where(m => m.StudentId != user.UserId)
                        .OrderBy(m => m.JoinedAt)
                        .FirstOrDefault();

                    if (replacementLeader == null)
                    {
                        // Requirement: If team has thesis and mentor, transfer authorid to mentor and set disbanded
                        var teamThesis = await _context.Theses.FirstOrDefaultAsync(t => t.TeamId == team.TeamId && t.SemesterId == team.SemesterId);
                        if (teamThesis != null && team.MentorId != null)
                        {
                            team.Status = CampusConstants.TeamStatus.Disbanded;
                            team.LeaderId = team.MentorId.Value; // Move leadership to mentor for the record
                            team.UpdatedAt = DateTime.UtcNow;
                            reassignedLeaderId = team.MentorId.Value;
                            teamRemoved = false;
                        }
                        else
                        {
                            _context.Teams.Remove(team);
                            teamRemoved = true;
                        }
                    }
                    else
                    {
                        var currentLeaderMember = team.Teammembers.FirstOrDefault(m => m.StudentId == user.UserId);
                        if (currentLeaderMember != null)
                        {
                            currentLeaderMember.Role = CampusConstants.TeamRole.Member;
                        }

                        replacementLeader.Role = CampusConstants.TeamRole.Leader;
                        team.LeaderId = replacementLeader.StudentId;
                        team.UpdatedAt = DateTime.UtcNow;
                        reassignedLeaderId = replacementLeader.StudentId;
                    }
                }

                if (teamRemoved) continue;

                await ReassignStudentArtifactsAsync(user.UserId, team.SemesterId, reassignedLeaderId);

                var invitations = await _context.Teaminvitations
                    .Where(i => i.TeamId == team.TeamId && (i.StudentId == user.UserId || i.InvitedBy == user.UserId))
                    .ToListAsync();

                if (invitations.Any())
                {
                    _context.Teaminvitations.RemoveRange(invitations);
                }

                var membership = team.Teammembers.FirstOrDefault(m => m.StudentId == user.UserId);
                if (membership != null)
                {
                    _context.Teammembers.Remove(membership);
                }

                int remainingMemberCount = team.Teammembers.Count(m => m.StudentId != user.UserId);
                
                // If this was the last member, handle removals or status update
                if (remainingMemberCount == 0)
                {
                    if (team.Status != CampusConstants.TeamStatus.Disbanded)
                    {
                        _context.Teams.Remove(team);
                    }
                    continue;
                }

                team.Status = remainingMemberCount switch
                {
                    >= 4 => CampusConstants.TeamStatus.Active,
                    3 => CampusConstants.TeamStatus.PendingApproval,
                    _ => CampusConstants.TeamStatus.Insufficient,
                };
            }

            var remainingInvitations = await _context.Teaminvitations
                .Where(i => i.StudentId == user.UserId || i.InvitedBy == user.UserId)
                .ToListAsync();
            if (remainingInvitations.Any())
            {
                _context.Teaminvitations.RemoveRange(remainingInvitations);
            }

            var remainingTheses = await _context.Theses
                .Where(t => t.UserId == user.UserId)
                .ToListAsync();
            if (remainingTheses.Any())
            {
                throw new InvalidOperationException($"Cannot delete student {user.Email} because some theses could not be reassigned automatically.");
            }

            var remainingHistories = await _context.ThesisHistories
                .Where(h => h.UploadedBy == user.UserId)
                .ToListAsync();
            if (remainingHistories.Any())
            {
                throw new InvalidOperationException($"Cannot delete student {user.Email} because thesis histories still reference the user.");
            }

            var studentWhitelists = await _context.Whitelists
                .Where(w => w.RoleId == studentRoleId &&
                            (w.Email == user.Email || (user.StudentCode != null && w.StudentCode == user.StudentCode)))
                .ToListAsync();

            if (studentWhitelists.Any())
            {
                _context.Whitelists.RemoveRange(studentWhitelists);
            }

            _context.Users.Remove(user);
        }

        private async Task ReassignStudentArtifactsAsync(int userId, int semesterId, int? targetUserId)
        {
            var semesterTheses = await _context.Theses
                .Where(t => t.UserId == userId && t.SemesterId == semesterId)
                .ToListAsync();

            var semesterThesisIds = await _context.Theses
                .Where(t => t.SemesterId == semesterId)
                .Select(t => t.ThesisId)
                .ToListAsync();

            var semesterHistories = await _context.ThesisHistories
                .Where(h => h.UploadedBy == userId && semesterThesisIds.Contains(h.ThesisId))
                .ToListAsync();

            if (!targetUserId.HasValue)
            {
                if (semesterTheses.Any() || semesterHistories.Any())
                {
                    throw new InvalidOperationException("Unable to reassign thesis ownership because no replacement leader is available.");
                }

                return;
            }

            foreach (var thesis in semesterTheses)
            {
                thesis.UserId = targetUserId.Value;
            }

            foreach (var history in semesterHistories)
            {
                history.UploadedBy = targetUserId.Value;
            }
        }

        private static Whitelist? FindMatchingWhitelist(WhitelistImportDTO importedItem, List<Whitelist> existingWhitelists, HashSet<int> matchedWhitelistIds)
        {
            var normalizedStudentCode = NormalizeKey(importedItem.StudentCode);
            if (!string.IsNullOrWhiteSpace(normalizedStudentCode))
            {
                var byStudentCode = existingWhitelists.FirstOrDefault(w =>
                    w.WhitelistId != 0 && !matchedWhitelistIds.Contains(w.WhitelistId) && NormalizeKey(w.StudentCode) == normalizedStudentCode);

                if (byStudentCode != null)
                {
                    return byStudentCode;
                }
            }

            var normalizedEmail = NormalizeEmail(importedItem.Email);
            return existingWhitelists.FirstOrDefault(w =>
                w.WhitelistId != 0 && !matchedWhitelistIds.Contains(w.WhitelistId) && NormalizeEmail(w.Email) == normalizedEmail);
        }

        private static User? FindMatchingUser(WhitelistImportDTO importedItem, List<User> existingUsers, HashSet<int> matchedUserIds)
        {
            var normalizedStudentCode = NormalizeKey(importedItem.StudentCode);
            if (!string.IsNullOrWhiteSpace(normalizedStudentCode))
            {
                var byStudentCode = existingUsers.FirstOrDefault(u =>
                    u.UserId != 0 && !matchedUserIds.Contains(u.UserId) && NormalizeKey(u.StudentCode) == normalizedStudentCode);

                if (byStudentCode != null)
                {
                    return byStudentCode;
                }
            }

            var normalizedEmail = NormalizeEmail(importedItem.Email);
            return existingUsers.FirstOrDefault(u =>
                u.UserId != 0 && !matchedUserIds.Contains(u.UserId) && NormalizeEmail(u.Email) == normalizedEmail);
        }

        private static string NormalizeEmail(string? email)
        {
            return email?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        private static string NormalizeKey(string? value)
        {
            return value?.Trim().ToLowerInvariant() ?? string.Empty;
        }
    }
}
