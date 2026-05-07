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
                .Include(u => u.CampusNavigation)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            return user?.CampusNavigation?.CampusName;
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

        public async Task<List<string>> ReconcileSemesterAsync(int semesterId, List<WhitelistImportDTO> importedItems, int studentRoleId, DateTime now)
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

                // Load whitelist rows from OTHER semesters that match the imported emails/codes.
                // These are loaded as tracked (no AsNoTracking) so that when the same student is
                // imported into a new semester, EF can UPDATE their existing row (reassigning SemesterId)
                // rather than attempting an INSERT that would violate the global unique-email index
                // (UQ__Whitelis__A9D10534BDF4FDF3) on the Whitelist table.
                var crossSemesterMatchingWhitelists = await _context.Whitelists
                    .Where(w => w.RoleId == studentRoleId &&
                                w.SemesterId != semesterId &&   // already in existingWhitelistsInSemester
                                (importedEmails.Contains(w.Email.ToLower()) ||
                                 (w.StudentCode != null && importedStudentCodes.Contains(w.StudentCode.ToLower()))))
                    .ToListAsync();

                // Match against both current-semester and cross-semester entries.
                // Current-semester entries are checked first (FindMatchingWhitelist order).
                // A cross-semester match will have its SemesterId updated to the target semester.
                var existingWhitelists = existingWhitelistsInSemester
                    .ToList();

                var candidateEmails = importedEmails
                    .Concat(crossSemesterMatchingWhitelists.Select(w => NormalizeEmail(w.Email)))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .ToList();

                var candidateStudentCodes = importedStudentCodes
                    .Concat(crossSemesterMatchingWhitelists.Select(w => NormalizeKey(w.StudentCode)))
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
                    
                    whitelistMatch.CampusId = importedItem.CampusId.Value;
                    whitelistMatch.SemesterId = semesterId;
                    whitelistMatch.AddedDate = whitelistMatch.AddedDate ?? now;
                    whitelistMatch.Status = CampusConstants.WhitelistStatus.Qualified;

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
                    userMatch.RoleId = studentRoleId;
                    userMatch.IsAuthorized = true;

                    if (userMatch.UserId != 0)
                    {
                        matchedUserIds.Add(userMatch.UserId);
                    }
                }

                // Logic refined: Only perform soft-deactivation (Status = Unqualified) for Students in an Active Semester
                var role = await _context.Roles.FindAsync(studentRoleId);
                var isStudentRole = role?.RoleName == CampusConstants.Roles.Student;
                
                var semester = await _context.Semesters.FindAsync(semesterId);
                var isActiveSemester = CampusConstants.SemesterStatus.IsOpenStage(semester?.Status);
                var unqualifiedEmails = new List<string>();
                if (isStudentRole && isActiveSemester)
                {
                    var whitelistsToMark = existingWhitelistsInSemester.Where(w => !matchedWhitelistIds.Contains(w.WhitelistId)).ToList();
                    foreach (var whitelistToMark in whitelistsToMark)
                    {
                        if (whitelistToMark.Status == CampusConstants.WhitelistStatus.Qualified)
                        {
                            unqualifiedEmails.Add(whitelistToMark.Email);
                        }
                        whitelistToMark.Status = CampusConstants.WhitelistStatus.Unqualified;
                    }

                    // --- [FIX] Deactivation Logic ---
                    // Previously, this only checked existingUsers which was limited to candidate emails.
                    // Now we explicitly find all Users whose whitelist entry just became Unqualified.
                    if (unqualifiedEmails.Any())
                    {
                        var emailsToMatch = unqualifiedEmails.Select(e => e.ToLower()).ToList();
                        var usersToDeactivate = await _context.Users
                            .Where(u => u.RoleId == studentRoleId && emailsToMatch.Contains(u.Email.ToLower()))
                            .ToListAsync();

                        foreach (var userToDeactivate in usersToDeactivate)
                        {
                            await DeactivateStudentUserAsync(userToDeactivate, studentRoleId, semesterId);
                        }
                    }

                    // --- Final synchronization pass for team statuses ---
                    // This ensures teams that might have had members removed or "un-removed" 
                    // always have the correct Qualified/Insufficient/Pending status.
                    var allTeamsInSemester = await _context.Teams
                        .Include(t => t.Teammembers)
                        .Where(t => t.SemesterId == semesterId && t.Status != CampusConstants.TeamStatus.Disbanded)
                        .ToListAsync();

                    foreach (var team in allTeamsInSemester)
                    {
                        int count = team.Teammembers.Count;
                        string newStatus = count switch
                        {
                            >= 5 => CampusConstants.TeamStatus.Active,
                            >= 3 => CampusConstants.TeamStatus.PendingApproval,
                            _ => CampusConstants.TeamStatus.Insufficient
                        };

                        if (team.Status != newStatus)
                        {
                            team.Status = newStatus;
                            team.UpdatedAt = now;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }
                return unqualifiedEmails;
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

        private async Task DeactivateStudentUserAsync(User user, int studentRoleId, int semesterId)
        {
            var relatedTeams = await _context.Teams
                .Include(t => t.Teammembers)
                .Where(t => t.SemesterId == semesterId && (t.LeaderId == user.UserId || t.Teammembers.Any(m => m.StudentId == user.UserId)))
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
                        var teamThesis = await _context.Theses.FirstOrDefaultAsync(t => t.TeamId == team.TeamId && t.SemesterId == team.SemesterId);
                        if (teamThesis != null && team.MentorId != null)
                        {
                            teamThesis.UserId = team.MentorId.Value;
                            teamThesis.UpdateDate = DateTime.UtcNow;
                            
                            team.Status = CampusConstants.TeamStatus.Disbanded;
                            team.UpdatedAt = DateTime.UtcNow;
                            _context.Teams.Remove(team);
                            teamRemoved = true;
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
                    .Where(i => i.TeamId == team.TeamId && (i.ReceiverId == user.UserId || i.InvitedBy == user.UserId))
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
                
                if (remainingMemberCount == 0)
                {
                    var teamThesis = await _context.Theses.FirstOrDefaultAsync(t => t.TeamId == team.TeamId && t.SemesterId == team.SemesterId);
                    if (teamThesis != null && team.MentorId != null)
                    {
                        teamThesis.UserId = team.MentorId.Value;
                        teamThesis.UpdateDate = DateTime.UtcNow;
                    }

                    _context.Teams.Remove(team);
                    continue;
                }

                team.Status = remainingMemberCount switch
                {
                    >= 5 => CampusConstants.TeamStatus.Active,
                    >= 3 => CampusConstants.TeamStatus.PendingApproval,
                    _ => CampusConstants.TeamStatus.Insufficient,
                };
            }

            var remainingInvitations = await _context.Teaminvitations
                .Where(i => i.ReceiverId == user.UserId || i.InvitedBy == user.UserId)
                .ToListAsync();
            if (remainingInvitations.Any())
            {
                _context.Teaminvitations.RemoveRange(remainingInvitations);
            }
            
            user.IsAuthorized = false;
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

        public async Task AddImportBatchAsync(ImportBatch batch)
        {
            _context.ImportBatches.Add(batch);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ImportBatch>> GetImportBatchesBySemesterAsync(int semesterId)
        {
            return await _context.ImportBatches
                .AsNoTracking()
                .Where(b => b.AffectedSemesterId == semesterId)
                .OrderByDescending(b => b.UploadedAt)
                .ToListAsync();
        }
    }
}


