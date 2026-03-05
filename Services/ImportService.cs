using System;
using System.Linq;
using System.Collections.Generic;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;
using Services.Helpers;

namespace Services
{
    public class ImportService : IImportService
    {
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ISemesterRepository _semesterRepository;

        public ImportService(IWhitelistRepository whitelistRepository, ISemesterRepository semesterRepository)
        {
            _whitelistRepository = whitelistRepository;
            _semesterRepository = semesterRepository;
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
                // nothing to save
                return;
            }

            var now = DateTime.UtcNow;

            // map DTOs to model entities
            var entities = items.Select(dto => new Whitelist
            {
                Email = dto.Email,
                StudentCode = dto.StudentCode,
                FullName = dto.FullName,
                RoleId = dto.RoleId,
                Campus = dto.Campus,
                SemesterId = dto.SemesterId,
                AddedDate = now
            }).ToList();

            // determine affected semesters and replace per-semester whitelist data
            var semesterIds = entities.Select(e => e.SemesterId).Where(s => s.HasValue).Select(s => s!.Value).Distinct();
            int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

            foreach (var semId in semesterIds)
            {
                var existing = await _whitelistRepository.GetBySemesterIdAsync(semId);
                if (existing != null && existing.Any())
                {
                    // CRITICAL FIX: Only delete existing students. Lecturers/Mentors are preserved.
                    var existingStudents = existing.Where(w => w.RoleId == studentRoleId).ToList();
                    if (existingStudents.Any())
                    {
                        await _whitelistRepository.DeleteRangeAsync(existingStudents);
                    }
                }

                var toAdd = entities.Where(e => e.SemesterId == semId).ToList();
                if (toAdd.Any())
                {
                    await _whitelistRepository.AddRangeAsync(toAdd);
                }
            }

            // Note: Import batch metadata (fileUrl, uploadedBy, version) is recorded by migration
            // but persisting a row in ImportBatches is not implemented here; consider adding
            // an ImportBatch repository/DAO to persist the fileUrl and version atomically with changes.
        }
    }
}
