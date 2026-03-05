using System;
using System.Linq;
using System.Collections.Generic;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.Extensions.Logging;
using Repositories;
using Services.Helpers;

namespace Services
{
    public class ImportService : IImportService
    {
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ILogger<ImportService> _logger;
        private readonly IRedisService _redisService;

        public ImportService(IWhitelistRepository whitelistRepository, ISemesterRepository semesterRepository, ILogger<ImportService> logger, IRedisService redisService)
        {
            _whitelistRepository = whitelistRepository;
            _semesterRepository = semesterRepository;
            _logger = logger;
            _redisService = redisService;
        }

        public async Task<ImportResult<WhitelistImportDTO>> ImportWhitelistFromExcel(Stream excelStream)
        {
            var importHelper = new Helpers.ImportHelper();
            var result = importHelper.ImportWhitelistFromExcel(excelStream);
            return await Task.FromResult(result);
        }

        public async Task SaveWhitelistBatchAsync(ImportResult<WhitelistImportDTO> importResult, string fileUrl, string? uploadedBy = null)
        {
            if (importResult == null) throw new ArgumentNullException(nameof(importResult));
            if (string.IsNullOrEmpty(fileUrl)) throw new ArgumentException("fileUrl is required", nameof(fileUrl));

            var items = importResult.Items ?? new List<WhitelistImportDTO>();
            if (!items.Any())
            {
                _logger.LogInformation("Import whitelist: No valid items to save. File: {fileUrl}", fileUrl);
                return;
            }

            _logger.LogInformation("Starting whitelist import. File: {fileUrl}, UploadedBy: {uploadedBy}, ItemCount: {itemCount}", 
                fileUrl, uploadedBy ?? "unknown", items.Count);

            try
            {
                var now = DateTime.UtcNow;

                // Validate all SemesterIds exist before proceeding
                var semesterIds = items.Select(dto => dto.SemesterId).Where(s => s.HasValue).Select(s => s!.Value).Distinct().ToList();

                foreach (var semId in semesterIds)
                {
                    var semesterExists = await _semesterRepository.SemesterExistsAsync(semId);
                    if (!semesterExists)
                    {
                        _logger.LogError("Import failed: SemesterId {semesterId} does not exist", semId);
                        throw new ArgumentException($"SemesterId {semId} does not exist in the system");
                    }
                }

                // Get the student role ID for all imports
                int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

                // map DTOs to model entities (all with student role)
                var entities = items.Select(dto => new Whitelist
                {
                    Email = dto.Email,
                    StudentCode = dto.StudentCode,
                    FullName = dto.FullName,
                    RoleId = studentRoleId,  // All imports are students
                    Campus = dto.Campus,
                    SemesterId = dto.SemesterId,
                    AddedDate = now
                }).ToList();

                // Process each semester to replace per-semester whitelist data
                int totalProcessed = 0;
                foreach (var semId in semesterIds)
                {
                    var toAdd = entities.Where(e => e.SemesterId == semId).ToList();
                    try
                    {
                        await _whitelistRepository.ReplaceStudentsBySemesterAsync(semId, studentRoleId, toAdd);
                        totalProcessed += toAdd.Count;
                        _logger.LogInformation("Successfully imported {count} whitelists for semester {semesterId}", toAdd.Count, semId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to import whitelists for semester {semesterId}. This semester import was rolled back.", semId);
                        throw;
                    }
                }

                _logger.LogInformation("Whitelist import completed successfully. File: {fileUrl}, TotalProcessed: {totalProcessed}, UploadedBy: {uploadedBy}", 
                    fileUrl, totalProcessed, uploadedBy ?? "unknown");

                // Invalidate semester cache since whitelist counts have changed
                await InvalidateSemesterCacheAsync(semesterIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Whitelist import failed. File: {fileUrl}, UploadedBy: {uploadedBy}", 
                    fileUrl, uploadedBy ?? "unknown");
                throw;
            }
        }

        private async Task InvalidateSemesterCacheAsync(List<int> semesterIds)
        {
            // Invalidate the "all semesters" cache since whitelist counts changed
            await _redisService.DeleteValueAsync("fctms:semester:all");

            // Also invalidate cache for specific semesters if they exist
            foreach (var semesterId in semesterIds)
            {
                await _redisService.DeleteValueAsync($"fctms:semester:id:{semesterId}");
            }

            _logger.LogInformation("Invalidated semester cache for {count} semesters", semesterIds.Count);
        }
    }
}
