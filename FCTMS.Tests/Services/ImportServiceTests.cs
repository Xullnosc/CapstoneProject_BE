using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using FluentAssertions;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories;
using Services;
using Services.Helpers;
using BusinessObjects.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FCTMS.Tests.Services
{
    public class ImportServiceTests
    {
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<ILogger<ImportService>> _mockLogger;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<ICampusContextService> _mockCampusContextService;

        public ImportServiceTests()
        {
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockLogger = new Mock<ILogger<ImportService>>();
            _mockRedisService = new Mock<IRedisService>();
            _mockCampusContextService = new Mock<ICampusContextService>();
            _mockCampusContextService.Setup(c => c.GetCurrentCampusId()).Returns(1);
            _mockRedisService.Setup(x => x.DeleteValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockRedisService.Setup(x => x.RemoveByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        private ImportService CreateService(FctmsContext context)
        {
            IImportRepository importRepository = new ImportRepository(new ImportDAO(context));
            return new ImportService(importRepository, _mockSemesterRepository.Object, _mockLogger.Object, _mockRedisService.Object, _mockCampusContextService.Object);
        }

        private static FctmsContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new FctmsContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static void SeedCampuses(FctmsContext context)
        {
            if (!context.Campuses.Any())
            {
                context.Campuses.Add(new Campus { CampusId = 1, CampusCode = "HL", CampusName = CampusConstants.HoaLac });
                context.Campuses.Add(new Campus { CampusId = 2, CampusCode = "DN", CampusName = CampusConstants.DaNang });
                context.SaveChanges();
            }
        }

        private static void SeedUploader(FctmsContext context, string email = "hod@example.com", int campusId = 1)
        {
            SeedCampuses(context);
            context.Users.Add(new User
            {
                Email = email,
                CampusId = campusId,
                RoleId = 1,
                IsAuthorized = true,
                CreatedAt = DateTime.UtcNow,
            });
            context.SaveChanges();
        }

        private void SetupSemester(string semesterCode, int semesterId)
        {
            _mockSemesterRepository.Setup(x => x.GetStudentRoleIdAsync()).ReturnsAsync(3);
            var semester = new Semester { SemesterId = semesterId, SemesterCode = semesterCode, CampusId = 1 };
            _mockSemesterRepository.Setup(x => x.GetSemesterByCodeAsync(semesterCode)).ReturnsAsync(semester);
            _mockSemesterRepository.Setup(x => x.GetSemesterByIdAsync(semesterId)).ReturnsAsync(semester);
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_ValidInput_UpsertsWhitelistAndUser()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);
            var service = CreateService(context);

            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO
                    {
                        RowNumber = 4,
                        Email = "student@example.com",
                        FullName = "Test User",
                        StudentCode = "ST001",
                        SemesterCode = "SP25"
                    }
                },
                Errors = new List<ImportError>()
            };

            await service.SaveWhitelistBatchAsync(importResult, 1, "test-file.xlsx", "test-file.xlsx", "hod@example.com");

            context.Whitelists.Should().ContainSingle();
            context.Users.Should().ContainSingle(user => user.Email == "student@example.com" && user.RoleId == 3 && user.CampusId == 1);
            context.Whitelists.Single().SemesterId.Should().Be(1);
            context.Whitelists.Single().CampusId.Should().Be(1);
            _mockRedisService.Verify(x => x.RemoveByPrefixAsync("fctms:semester:", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_NoItems_ReturnsEarlyWithoutInvalidatingCache()
        {
            using var context = CreateContext();
            SeedUploader(context);
            var service = CreateService(context);

            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>(),
                Errors = new List<ImportError>()
            };

            await service.SaveWhitelistBatchAsync(importResult, 1, "test-file.xlsx", "test-file.xlsx", "hod@example.com");

            _mockRedisService.Verify(x => x.RemoveByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_MarkedConflict_SkipsAndAddsError()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);
            context.Users.Add(new User
            {
                Email = "lecturer@example.com",
                FullName = "Existing Lecturer",
                RoleId = 2,
                CampusId = 1,
                IsAuthorized = true,
                CreatedAt = DateTime.UtcNow,
            });
            context.SaveChanges();

            var service = CreateService(context);
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO
                    {
                        RowNumber = 4,
                        Email = "lecturer@example.com",
                        FullName = "Conflict User",
                        StudentCode = "ST099",
                        SemesterCode = "SP25",
                        IsMarked = true,
                        MarkedReason = "Role conflict",
                        ExistingRole = "Lecturer"
                    }
                },
                Errors = new List<ImportError>()
            };

            await service.SaveWhitelistBatchAsync(importResult, 1, "test-file.xlsx", "test-file.xlsx", "hod@example.com");

            context.Whitelists.Should().BeEmpty();
            context.Users.Count(user => user.Email == "lecturer@example.com").Should().Be(1);
            importResult.Errors.Should().ContainSingle(e => e.Message.Contains("Skipped") && e.Message.Contains("Role conflict"));
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_ExistingStudentMatchedByEmail_UpdatesStudentCode()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);
            context.Users.Add(new User
            {
                Email = "student@example.com",
                FullName = "Old Name",
                StudentCode = "OLD001",
                RoleId = 3,
                CampusId = 2,
                IsAuthorized = true,
                CreatedAt = DateTime.UtcNow,
            });
            context.Whitelists.Add(new Whitelist
            {
                Email = "student@example.com",
                FullName = "Old Name",
                StudentCode = "OLD001",
                RoleId = 3,
                CampusId = 2,
                SemesterId = 1,
                AddedDate = DateTime.UtcNow,
            });
            context.SaveChanges();

            var service = CreateService(context);
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO
                    {
                        RowNumber = 4,
                        Email = "student@example.com",
                        FullName = "Updated Name",
                        StudentCode = "NEW001",
                        SemesterCode = "SP25"
                    }
                }
            };

            await service.SaveWhitelistBatchAsync(importResult, 1, "test-file.xlsx", "test-file.xlsx", "hod@example.com");

            context.Users.Should().ContainSingle(user => user.Email == "student@example.com" && user.StudentCode == "NEW001" && user.FullName == "Updated Name");
            context.Whitelists.Should().ContainSingle(whitelist => whitelist.Email == "student@example.com" && whitelist.StudentCode == "NEW001");
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_RemovesStudentsNotPresentInImport()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);

            context.Users.AddRange(
                new User
                {
                    Email = "keep@example.com",
                    FullName = "Keep User",
                    StudentCode = "ST001",
                    RoleId = 3,
                    CampusId = 1,
                    IsAuthorized = true,
                    CreatedAt = DateTime.UtcNow,
                },
                new User
                {
                    Email = "remove@example.com",
                    FullName = "Remove User",
                    StudentCode = "ST002",
                    RoleId = 3,
                    CampusId = 1,
                    IsAuthorized = true,
                    CreatedAt = DateTime.UtcNow,
                });

            context.Whitelists.AddRange(
                new Whitelist
                {
                    Email = "keep@example.com",
                    FullName = "Keep User",
                    StudentCode = "ST001",
                    RoleId = 3,
                    CampusId = 1,
                    SemesterId = 1,
                    AddedDate = DateTime.UtcNow,
                },
                new Whitelist
                {
                    Email = "remove@example.com",
                    FullName = "Remove User",
                    StudentCode = "ST002",
                    RoleId = 3,
                    CampusId = 1,
                    SemesterId = 1,
                    AddedDate = DateTime.UtcNow,
                });
            context.SaveChanges();

            var service = CreateService(context);
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO
                    {
                        RowNumber = 4,
                        Email = "keep@example.com",
                        FullName = "Keep User",
                        StudentCode = "ST001",
                        SemesterCode = "SP25"
                    }
                }
            };

            await service.SaveWhitelistBatchAsync(importResult, 1, "test-file.xlsx", "test-file.xlsx", "hod@example.com");

            context.Users.Should().ContainSingle(user => user.Email == "keep@example.com");
            context.Whitelists.Should().ContainSingle(whitelist => whitelist.Email == "keep@example.com");
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_DuplicateEmailInImport_SkipsDuplicateAndAddsError()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);
            var service = CreateService(context);

            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO
                    {
                        RowNumber = 4,
                        Email = "duplicate@example.com",
                        FullName = "First Student",
                        StudentCode = "ST001",
                        SemesterCode = "SP25"
                    },
                    new WhitelistImportDTO
                    {
                        RowNumber = 5,
                        Email = "duplicate@example.com",
                        FullName = "Second Student",
                        StudentCode = "ST002",
                        SemesterCode = "SP25"
                    }
                },
                Errors = new List<ImportError>()
            };

            await service.SaveWhitelistBatchAsync(importResult, 1, "test-file.xlsx", "test-file.xlsx", "hod@example.com");

            context.Whitelists.Should().ContainSingle(w => w.Email == "duplicate@example.com");
            context.Users.Should().ContainSingle(u => u.Email == "duplicate@example.com");
            importResult.Errors.Should().ContainSingle(e =>
                e.Row == 5 &&
                e.Column == CampusConstants.WhitelistImportColumns.Email &&
                e.Message.Contains("Duplicate email in import file"));
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_ExistingStudentInAnotherSemester_ReusesWhitelistRow()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);

            context.Users.Add(new User
            {
                Email = "student@example.com",
                FullName = "Existing Student",
                StudentCode = "ST001",
                RoleId = 3,
                CampusId = 1,
                IsAuthorized = true,
                CreatedAt = DateTime.UtcNow,
            });

            context.Whitelists.Add(new Whitelist
            {
                Email = "student@example.com",
                FullName = "Existing Student",
                StudentCode = "ST001",
                RoleId = 3,
                CampusId = 1,
                SemesterId = 2,
                AddedDate = DateTime.UtcNow,
            });
            context.SaveChanges();

            var service = CreateService(context);
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO
                    {
                        RowNumber = 4,
                        Email = "student@example.com",
                        FullName = "Imported Student",
                        StudentCode = "ST001",
                        SemesterCode = "SP25"
                    }
                },
                Errors = new List<ImportError>()
            };

            await service.SaveWhitelistBatchAsync(importResult, 1, "test-file.xlsx", "test-file.xlsx", "hod@example.com");

            // The existing whitelist row (previously in semester 2) is moved to semester 1 via UPDATE,
            // not inserted as a new row. The global unique index on Email means only one row may exist.
            context.Whitelists.Count(w => w.Email == "student@example.com").Should().Be(1);
            var movedRow = context.Whitelists.Single(w => w.Email == "student@example.com");
            movedRow.SemesterId.Should().Be(1);
            movedRow.FullName.Should().Be("Imported Student");
        }


        #region SaveWhitelistBatchAsync - Logging Verification

        [Fact]
        public async Task SaveWhitelistBatchAsync_LogsStartAndCompletion()
        {
            // Arrange
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);
            var service = CreateService(context);

            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO
                    {
                        RowNumber = 4,
                        Email = "test@example.com",
                        FullName = "Test User",
                        StudentCode = "ST001",
                        SemesterCode = "SP25"
                    }
                },
                Errors = new List<ImportError>()
            };

            // Act
            await service.SaveWhitelistBatchAsync(importResult, 1, "test-file.xlsx", "test-file.xlsx", "hod@example.com");

            // Assert - verify logging occurred at start and end
            _mockLogger.Verify(
                x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(2),
                "Should log at least start and completion");
        }

        #endregion

        #region ImportWhitelistFromExcel

        /// <summary>
        /// Creates an in-memory Excel stream containing the required headers (row 3)
        /// and optional data rows. The <paramref name="configureRows"/> callback receives
        /// the worksheet and can populate every data row starting at row 4.
        /// </summary>
        private static byte[] CreateMinimalExcelBytes(Action<OfficeOpenXml.ExcelWorksheet> configureRows)
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("Capstone Project");

            using var package = new OfficeOpenXml.ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Import");

            // Row 3 = headers (ImportHelper starts reading headers from column 2)
            ws.Cells[3, 2].Value = CampusConstants.WhitelistImportColumns.Email;
            ws.Cells[3, 3].Value = CampusConstants.WhitelistImportColumns.StudentCode;
            ws.Cells[3, 4].Value = CampusConstants.WhitelistImportColumns.FullName;

            configureRows(ws);

            return package.GetAsByteArray();
        }

        [Fact]
        public async Task ImportWhitelistFromExcel_ConflictingRow_IsMarked()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);

            // Existing non-student user whose email will be in the Excel (triggers the conflict).
            context.Users.Add(new User
            {
                Email = "lecturer@example.com",
                FullName = "Lecturer Name",
                RoleId = 2,
                CampusId = 1,
                IsAuthorized = true,
                CreatedAt = DateTime.UtcNow,
            });
            context.SaveChanges();

            var service = CreateService(context);

            var excelBytes = CreateMinimalExcelBytes(ws =>
            {
                ws.Cells[4, 2].Value = "lecturer@example.com";
                ws.Cells[4, 3].Value = "ST001";
                ws.Cells[4, 4].Value = "Conflict Student";
            });

            using var stream = new MemoryStream(excelBytes);
            var result = await service.ImportWhitelistFromExcel(stream, 1, "hod@example.com");

            result.Items.Should().ContainSingle(item => item.Email == "lecturer@example.com" && item.IsMarked == true);
        }

        [Fact]
        public async Task ImportWhitelistFromExcel_WithRowOverrideThatResolvesConflict_RowIsNotMarked()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);

            // Existing non-student user whose email will be in the Excel.
            context.Users.Add(new User
            {
                Email = "lecturer@example.com",
                FullName = "Lecturer Name",
                RoleId = 2,
                CampusId = 1,
                IsAuthorized = true,
                CreatedAt = DateTime.UtcNow,
            });
            context.SaveChanges();

            var service = CreateService(context);

            var excelBytes = CreateMinimalExcelBytes(ws =>
            {
                ws.Cells[4, 2].Value = "lecturer@example.com";
                ws.Cells[4, 3].Value = "ST001";
                ws.Cells[4, 4].Value = "Conflict Student";
            });

            // Override row 4 to use a different email that does not conflict.
            var overrides = new List<WhitelistRowOverrideDTO>
            {
                new WhitelistRowOverrideDTO
                {
                    RowNumber = 4,
                    Email = "newstudent@example.com",
                    FullName = "Resolved Student",
                }
            };

            using var stream = new MemoryStream(excelBytes);
            var result = await service.ImportWhitelistFromExcel(stream, 1, "hod@example.com", overrides);

            result.Items.Should().ContainSingle();
            result.Items.Single().IsMarked.Should().BeFalse("the override replaced the conflicting email");
            result.Items.Single().Email.Should().Be("newstudent@example.com");
            result.Items.Single().FullName.Should().Be("Resolved Student");
        }

        [Fact]
        public async Task ImportWhitelistFromExcel_OverrideDoesNotResolveConflict_RowRemainsMarked()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);

            // Two existing non-student users.
            context.Users.AddRange(
                new User { Email = "lecturer1@example.com", FullName = "Lecturer 1", RoleId = 2, CampusId = 1, IsAuthorized = true, CreatedAt = DateTime.UtcNow },
                new User { Email = "lecturer2@example.com", FullName = "Lecturer 2", RoleId = 2, CampusId = 1, IsAuthorized = true, CreatedAt = DateTime.UtcNow }
            );
            context.SaveChanges();

            var service = CreateService(context);

            var excelBytes = CreateMinimalExcelBytes(ws =>
            {
                ws.Cells[4, 2].Value = "lecturer1@example.com";
                ws.Cells[4, 3].Value = "ST001";
                ws.Cells[4, 4].Value = "Conflict Student";
            });

            // Override still provides another conflicting email (also a non-student).
            var overrides = new List<WhitelistRowOverrideDTO>
            {
                new WhitelistRowOverrideDTO { RowNumber = 4, Email = "lecturer2@example.com" }
            };

            using var stream = new MemoryStream(excelBytes);
            var result = await service.ImportWhitelistFromExcel(stream, 1, "hod@example.com", overrides);

            result.Items.Single().IsMarked.Should().BeTrue("the override email still belongs to a non-student role");
        }

        [Fact]
        public async Task ImportWhitelistFromExcel_HeaderAliases_ParsesSuccessfully()
        {
            using var context = CreateContext();
            SeedUploader(context);
            SetupSemester("SP25", 1);

            var service = CreateService(context);

            OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("Capstone Project");
            using var package = new OfficeOpenXml.ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Import");

            // Use alias-style headers.
            ws.Cells[3, 2].Value = "E-mail";
            ws.Cells[3, 3].Value = "Student Code";
            ws.Cells[3, 4].Value = "Full Name";

            ws.Cells[4, 2].Value = "student1@example.com";
            ws.Cells[4, 3].Value = "ST100";
            ws.Cells[4, 4].Value = "Student One";

            using var stream = new MemoryStream(package.GetAsByteArray());
            var result = await service.ImportWhitelistFromExcel(stream, 1, "hod@example.com");

            result.Items.Should().ContainSingle();
            result.Items.Single().Email.Should().Be("student1@example.com");
            result.Items.Single().StudentCode.Should().Be("ST100");
            result.Items.Single().FullName.Should().Be("Student One");
            result.Errors.Should().NotContain(e => e.Message.Contains("Missing required column", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region GetImportBatchesBySemesterAsync

        [Fact]
        public async Task GetImportBatchesBySemesterAsync_ReturnsBatches()
        {
            using var context = CreateContext();
            var service = CreateService(context);

            context.ImportBatches.AddRange(
                new ImportBatch { ImportBatchId = 1, FileUrl = "url1", AffectedSemesterId = 1, UploadedAt = DateTime.UtcNow, Version = 1 },
                new ImportBatch { ImportBatchId = 2, FileUrl = "url2", AffectedSemesterId = 1, UploadedAt = DateTime.UtcNow.AddMinutes(5), Version = 1 },
                new ImportBatch { ImportBatchId = 3, FileUrl = "url3", AffectedSemesterId = 2, UploadedAt = DateTime.UtcNow, Version = 1 }
            );
            context.SaveChanges();

            var result = await service.GetImportBatchesBySemesterAsync(1);

            result.Should().HaveCount(2);
            result.Select(r => r.FileUrl).Should().Contain(new[] { "url1", "url2" });
            result.Should().NotContain(r => r.FileUrl == "url3");
        }

        [Fact]
        public async Task GetImportBatchesBySemesterAsync_NoBatches_ReturnsEmpty()
        {
            using var context = CreateContext();
            var service = CreateService(context);

            var result = await service.GetImportBatchesBySemesterAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetImportBatchesBySemesterAsync_Ordering_ReturnsDescendingByUploadedAt()
        {
            using var context = CreateContext();
            var service = CreateService(context);

            context.ImportBatches.AddRange(
                new ImportBatch { ImportBatchId = 1, FileUrl = "url1", AffectedSemesterId = 1, UploadedAt = DateTime.UtcNow, Version = 1 },
                new ImportBatch { ImportBatchId = 2, FileUrl = "url2", AffectedSemesterId = 1, UploadedAt = DateTime.UtcNow.AddMinutes(5), Version = 2 },
                new ImportBatch { ImportBatchId = 3, FileUrl = "url3", AffectedSemesterId = 1, UploadedAt = DateTime.UtcNow.AddMinutes(10), Version = 3 }
            );
            context.SaveChanges();

            var result = await service.GetImportBatchesBySemesterAsync(1);

            result.Should().HaveCount(3);
            result.First().FileUrl.Should().Be("url3"); // Newest
            result.Last().FileUrl.Should().Be("url1"); // Oldest
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_NullImportResult_ThrowsArgumentNullException()
        {
            using var context = CreateContext();
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                service.SaveWhitelistBatchAsync(null!, 1, "urllink", "test.xlsx", "user@example.com"));
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_EmptyFileUrl_ThrowsArgumentException()
        {
            using var context = CreateContext();
            var service = CreateService(context);
            var importResult = new ImportResult<WhitelistImportDTO> { Items = new List<WhitelistImportDTO>(), Errors = new List<ImportError>() };

            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.SaveWhitelistBatchAsync(importResult, 1, "", "test.xlsx", "user@example.com"));
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_NullOriginalFileName_ThrowsArgumentException()
        {
            using var context = CreateContext();
            var service = CreateService(context);
            var importResult = new ImportResult<WhitelistImportDTO> { Items = new List<WhitelistImportDTO>(), Errors = new List<ImportError>() };

            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.SaveWhitelistBatchAsync(importResult, 1, "url", null!, "user@example.com"));
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_MissingUploaderEmail_ThrowsArgumentException()
        {
            using var context = CreateContext();
            var service = CreateService(context);
            var importResult = new ImportResult<WhitelistImportDTO> { Items = new List<WhitelistImportDTO>(), Errors = new List<ImportError>() };

            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.SaveWhitelistBatchAsync(importResult, 1, "url", "file.xlsx", " "));
        }



        [Fact]
        public async Task SaveWhitelistBatchAsync_NoItems_DoesNotCreateBatch()
        {
            using var context = CreateContext();
            var service = CreateService(context);
            var importResult = new ImportResult<WhitelistImportDTO> { Items = new List<WhitelistImportDTO>(), Errors = new List<ImportError>() };

            await service.SaveWhitelistBatchAsync(importResult, 1, "url", "file.xlsx", "user@example.com");

            context.ImportBatches.Should().BeEmpty();
        }

        [Fact]
        public async Task GetImportBatchesBySemesterAsync_Mapping_AllPropertiesMapped()
        {
            using var context = CreateContext();
            var service = CreateService(context);
            var now = DateTime.UtcNow;

            context.ImportBatches.Add(new ImportBatch 
            { 
                ImportBatchId = 10, 
                FileUrl = "url", 
                OriginalFileName = "name.xlsx", 
                AffectedSemesterId = 1, 
                UploadedAt = now, 
                Version = 2 
            });
            context.SaveChanges();

            var results = await service.GetImportBatchesBySemesterAsync(1);
            var res = results.First();

            res.ImportBatchId.Should().Be(10);
            res.FileUrl.Should().Be("url");
            res.OriginalFileName.Should().Be("name.xlsx");
            res.UploadedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        }



        #endregion
    }
}
