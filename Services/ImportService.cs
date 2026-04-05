using System;
using System.Linq;
using System.Collections.Generic;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.Extensions.Logging;
using Repositories;
using Services.Helpers;
using BusinessObjects.Interfaces;

namespace Services
{
    public class ImportService : IImportService
    {
        private readonly IImportRepository _importRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ILogger<ImportService> _logger;
        private readonly IRedisService _redisService;
        private readonly ICampusContextService _campusContextService;

        public ImportService(
            IImportRepository importRepository,
            ISemesterRepository semesterRepository,
            ILogger<ImportService> logger,
            IRedisService redisService,
            ICampusContextService campusContextService)
        {
            _importRepository = importRepository;
            _semesterRepository = semesterRepository;
            _logger = logger;
            _redisService = redisService;
            _campusContextService = campusContextService;
        }

        public async Task<ImportResult<WhitelistImportDTO>> ImportWhitelistFromExcel(
            Stream excelStream,
            int semesterId,
            string uploaderEmail,
            List<WhitelistRowOverrideDTO>? rowOverrides = null)
        {
            var importHelper = new Helpers.ImportHelper();
            var result = importHelper.ImportWhitelistFromExcel(excelStream);

            // Apply HOD-supplied row-level corrections before validation/conflict-check.
            if (rowOverrides != null && rowOverrides.Count > 0)
            {
                var overrideMap = rowOverrides
                    .Where(o => o.RowNumber > 0)
                    .ToDictionary(o => o.RowNumber);

                foreach (var item in result.Items)
                {
                    if (!overrideMap.TryGetValue(item.RowNumber, out var ov)) continue;

                    if (!string.IsNullOrWhiteSpace(ov.Email))
                        item.Email = ov.Email.Trim();
                    if (!string.IsNullOrWhiteSpace(ov.FullName))
                        item.FullName = ov.FullName.Trim();
                    if (!string.IsNullOrWhiteSpace(ov.StudentCode))
                        item.StudentCode = ov.StudentCode.Trim();
                }
            }

            return await PrepareImportResultAsync(result, semesterId, uploaderEmail);
        }

        public async Task SaveWhitelistBatchAsync(ImportResult<WhitelistImportDTO> importResult, int semesterId, string fileUrl, string originalFileName, string uploaderEmail)
        {
            if (importResult == null) throw new ArgumentNullException(nameof(importResult));
            if (string.IsNullOrEmpty(fileUrl)) throw new ArgumentException("fileUrl cannot be empty", nameof(fileUrl));
            if (string.IsNullOrEmpty(originalFileName)) throw new ArgumentException("originalFileName cannot be empty", nameof(originalFileName));
            if (string.IsNullOrWhiteSpace(uploaderEmail)) throw new ArgumentException("uploaderEmail is required", nameof(uploaderEmail));

            if (importResult.Items == null || !importResult.Items.Any()) return;

            var preparedResult = await PrepareImportResultAsync(importResult, semesterId, uploaderEmail);

            // Filter out marked (conflicting) items — they'll appear in errors but won't block the rest
            var markedItems = preparedResult.Items.Where(item => item.IsMarked).ToList();
            if (markedItems.Any())
            {
                foreach (var marked in markedItems)
                {
                    preparedResult.Errors.Add(new ImportError
                    {
                        Row = marked.RowNumber,
                        Column = "Email/StudentCode",
                        Message = $"Skipped: {marked.MarkedReason} (Existing role: {marked.ExistingRole})"
                    });
                }
                preparedResult.Items = preparedResult.Items.Where(item => !item.IsMarked).ToList();
            }

            var items = preparedResult.Items ?? new List<WhitelistImportDTO>();
            if (!items.Any())
            {
                _logger.LogInformation("Import whitelist: No valid items to save. File: {fileUrl}", fileUrl);
                return;
            }

            _logger.LogInformation("Starting whitelist import. File: {fileUrl}, UploadedBy: {uploadedBy}, ItemCount: {itemCount}", 
                fileUrl, uploaderEmail, items.Count);

            try
            {
                var now = DateTime.UtcNow;
                int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

                var semesterIds = items
                    .Where(dto => dto.SemesterId.HasValue)
                    .Select(dto => dto.SemesterId!.Value)
                    .Distinct()
                    .ToList();

                int totalProcessed = 0;
                foreach (var semesterGroup in items.GroupBy(item => item.SemesterId!.Value))
                {
                    try
                    {
                        await _importRepository.ReconcileSemesterAsync(semesterGroup.Key, semesterGroup.ToList(), studentRoleId, now);
                        totalProcessed += semesterGroup.Count();
                        _logger.LogInformation("Successfully reconciled {count} whitelist rows for semester {semesterId}", semesterGroup.Count(), semesterGroup.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to reconcile whitelist import for semester {semesterId}. Entire import was rolled back.", semesterGroup.Key);
                        throw;
                    }
                }

                _logger.LogInformation("Whitelist import completed successfully. File: {fileUrl}, TotalProcessed: {totalProcessed}, UploadedBy: {uploadedBy}", 
                    fileUrl, totalProcessed, uploaderEmail);

                // Save Import Batch Record
                var batchRecord = new ImportBatch
                {
                    FileUrl = fileUrl,
                    OriginalFileName = originalFileName,
                    UploadedBy = uploaderEmail,
                    UploadedAt = now,
                    AffectedSemesterId = semesterId,
                    Version = 1,
                    Notes = $"Imported {totalProcessed} rows"
                };
                await _importRepository.AddImportBatchAsync(batchRecord);

                await InvalidateSemesterCacheAsync(semesterIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Whitelist import failed. File: {fileUrl}, UploadedBy: {uploadedBy}", 
                    fileUrl, uploaderEmail);
                throw;
            }
        }

        private async Task<ImportResult<WhitelistImportDTO>> PrepareImportResultAsync(ImportResult<WhitelistImportDTO> importResult, int semesterId, string uploaderEmail)
        {
            if (importResult == null) throw new ArgumentNullException(nameof(importResult));

            var items = importResult.Items ?? new List<WhitelistImportDTO>();
            if (!items.Any())
            {
                return importResult;
            }

            var semester = await _semesterRepository.GetSemesterByIdAsync(semesterId)
                ?? throw new KeyNotFoundException($"Semester with id {semesterId} does not exist.");

            int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

            var validItems = new List<WhitelistImportDTO>();
            var seenEmails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var seenStudentCodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                item.Email = item.Email.Trim();
                item.StudentCode = item.StudentCode?.Trim();
                item.FullName = item.FullName?.Trim();
                item.SemesterId = semester.SemesterId;
                item.SemesterCode = semester.SemesterCode;
                item.SemesterName = semester.SemesterName;
                item.CampusId = semester.CampusId;
                item.Campus = CampusConstants.MapIdToFullName(semester.CampusId);
                item.RoleId = studentRoleId;
                item.Role = CampusConstants.Roles.Student;
                item.IsMarked = false;
                item.ExistingRole = null;
                item.MarkedReason = null;

                var normalizedEmail = NormalizeEmail(item.Email);
                if (!string.IsNullOrWhiteSpace(normalizedEmail) && seenEmails.TryGetValue(normalizedEmail, out var existingEmailRow))
                {
                    importResult.Errors.Add(new ImportError
                    {
                        Row = item.RowNumber,
                        Column = CampusConstants.WhitelistImportColumns.Email,
                        Message = $"Duplicate email in import file. This email already appears at row {existingEmailRow}."
                    });
                    continue;
                }

                var normalizedStudentCode = NormalizeKey(item.StudentCode);
                if (!string.IsNullOrWhiteSpace(normalizedStudentCode) && seenStudentCodes.TryGetValue(normalizedStudentCode, out var existingStudentCodeRow))
                {
                    importResult.Errors.Add(new ImportError
                    {
                        Row = item.RowNumber,
                        Column = CampusConstants.WhitelistImportColumns.StudentCode,
                        Message = $"Duplicate student code in import file. This student code already appears at row {existingStudentCodeRow}."
                    });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(normalizedEmail))
                {
                    seenEmails[normalizedEmail] = item.RowNumber;
                }
                if (!string.IsNullOrWhiteSpace(normalizedStudentCode))
                {
                    seenStudentCodes[normalizedStudentCode] = item.RowNumber;
                }
                validItems.Add(item);
            }

            importResult.Items = validItems;
            await MarkBlockingConflictsAsync(importResult.Items, studentRoleId);
            return importResult;
        }

        private async Task MarkBlockingConflictsAsync(List<WhitelistImportDTO> items, int studentRoleId)
        {
            if (!items.Any())
            {
                return;
            }

            var normalizedEmails = items
                .Select(item => NormalizeEmail(item.Email))
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct()
                .ToList();

            var normalizedStudentCodes = items
                .Select(item => NormalizeKey(item.StudentCode))
                .Where(studentCode => !string.IsNullOrWhiteSpace(studentCode))
                .Distinct()
                .ToList();

            var existingUsers = await _importRepository.GetUsersForConflictCheckAsync(normalizedEmails, normalizedStudentCodes);
            var existingWhitelists = await _importRepository.GetWhitelistsForConflictCheckAsync(normalizedEmails, normalizedStudentCodes);

            foreach (var item in items)
            {
                var normalizedEmail = NormalizeEmail(item.Email);
                var normalizedStudentCode = NormalizeKey(item.StudentCode);

                var conflictingUser = existingUsers.FirstOrDefault(user =>
                    user.RoleId != studentRoleId &&
                    (NormalizeEmail(user.Email) == normalizedEmail || NormalizeKey(user.StudentCode) == normalizedStudentCode));

                if (conflictingUser != null)
                {
                    MarkItem(item, conflictingUser.Role?.RoleName ?? $"RoleId {conflictingUser.RoleId}", "Role conflict");
                    continue;
                }

                var conflictingWhitelist = existingWhitelists.FirstOrDefault(whitelist =>
                    whitelist.RoleId != studentRoleId &&
                    (NormalizeEmail(whitelist.Email) == normalizedEmail || NormalizeKey(whitelist.StudentCode) == normalizedStudentCode));

                if (conflictingWhitelist != null)
                {
                    MarkItem(item, conflictingWhitelist.Role?.RoleName ?? $"RoleId {conflictingWhitelist.RoleId}", "Role conflict");
                }
            }
        }

        private static void MarkItem(WhitelistImportDTO item, string existingRole, string reason)
        {
            item.IsMarked = true;
            item.ExistingRole = existingRole;
            item.MarkedReason = reason;
        }

        private static string NormalizeEmail(string? email)
        {
            return email?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        private static string NormalizeKey(string? value)
        {
            return value?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        private async Task InvalidateSemesterCacheAsync(List<int> semesterIds)
        {
            // Clear all semester-related caches (all campuses)
            await _redisService.RemoveByPrefixAsync("fctms:semester:");
            _logger.LogInformation("Invalidated all semester caches after whitelist import");
        }

        public async Task<List<ImportBatchDTO>> GetImportBatchesBySemesterAsync(int semesterId)
        {
            var batches = await _importRepository.GetImportBatchesBySemesterAsync(semesterId);
            return batches.Select(b => new ImportBatchDTO
            {
                ImportBatchId = b.ImportBatchId,
                FileUrl = b.FileUrl,
                OriginalFileName = b.OriginalFileName,
                UploadedBy = b.UploadedBy,
                UploadedAt = b.UploadedAt,
                AffectedSemesterId = b.AffectedSemesterId,
                Version = b.Version,
                Notes = b.Notes
            }).ToList();
        }
    }
}
