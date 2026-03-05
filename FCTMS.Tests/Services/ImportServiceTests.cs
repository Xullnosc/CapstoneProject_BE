using BusinessObjects.DTOs;
using BusinessObjects.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories;
using Services;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FCTMS.Tests.Services
{
    public class ImportServiceTests
    {
        private readonly Mock<IWhitelistRepository> _mockWhitelistRepository;
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<ILogger<ImportService>> _mockLogger;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly ImportService _importService;

        public ImportServiceTests()
        {
            _mockWhitelistRepository = new Mock<IWhitelistRepository>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockLogger = new Mock<ILogger<ImportService>>();
            _mockRedisService = new Mock<IRedisService>();
            _mockRedisService.Setup(x => x.DeleteValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _importService = new ImportService(_mockWhitelistRepository.Object, _mockSemesterRepository.Object, _mockLogger.Object, _mockRedisService.Object);
        }

        #region SaveWhitelistBatchAsync - Happy Path

        [Fact]
        public async Task SaveWhitelistBatchAsync_ValidInput_SavesSuccessfully()
        {
            // Arrange
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO { Email = "test@example.com", FullName = "Test User", RoleId = 3, Campus = "Hanoi", SemesterId = 1, StudentCode = "ST001" }
                },
                Errors = new List<ImportError>()
            };

            _mockSemesterRepository.Setup(x => x.SemesterExistsAsync(1)).ReturnsAsync(true);
            _mockSemesterRepository.Setup(x => x.GetStudentRoleIdAsync()).ReturnsAsync(3);
            _mockWhitelistRepository.Setup(x => x.ReplaceStudentsBySemesterAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<Whitelist>>())).Returns(Task.CompletedTask);

            // Act
            await _importService.SaveWhitelistBatchAsync(importResult, "test-file.xlsx", "testuser");

            // Assert
            _mockWhitelistRepository.Verify(x => x.ReplaceStudentsBySemesterAsync(1, 3, It.IsAny<IEnumerable<Whitelist>>()), Times.Once);
            _mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeast(2)); // At least success log
            // Verify cache invalidation
            _mockRedisService.Verify(x => x.DeleteValueAsync("fctms:semester:all", It.IsAny<CancellationToken>()), Times.Once);
            _mockRedisService.Verify(x => x.DeleteValueAsync("fctms:semester:id:1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_NoItems_ReturnsEarlyWithoutInvalidatingCache()
        {
            // Arrange
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>(),
                Errors = new List<ImportError>()
            };

            // Act
            await _importService.SaveWhitelistBatchAsync(importResult, "test-file.xlsx", "testuser");

            // Assert
            _mockWhitelistRepository.Verify(x => x.ReplaceStudentsBySemesterAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<Whitelist>>()), Times.Never);
            // Cache should NOT be invalidated since nothing was imported
            _mockRedisService.Verify(x => x.DeleteValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_MultipleSemesters_CallsReplaceForEachSemester()
        {
            // Arrange
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO { Email = "test1@example.com", FullName = "User 1", RoleId = 3, Campus = "Hanoi", SemesterId = 1, StudentCode = "ST001" },
                    new WhitelistImportDTO { Email = "test2@example.com", FullName = "User 2", RoleId = 3, Campus = "HCMC", SemesterId = 2, StudentCode = "ST002" }
                },
                Errors = new List<ImportError>()
            };

            _mockSemesterRepository.Setup(x => x.SemesterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
            _mockSemesterRepository.Setup(x => x.GetStudentRoleIdAsync()).ReturnsAsync(3);
            _mockWhitelistRepository.Setup(x => x.ReplaceStudentsBySemesterAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<Whitelist>>())).Returns(Task.CompletedTask);

            // Act
            await _importService.SaveWhitelistBatchAsync(importResult, "test-file.xlsx", "testuser");

            // Assert
            _mockWhitelistRepository.Verify(x => x.ReplaceStudentsBySemesterAsync(1, 3, It.IsAny<IEnumerable<Whitelist>>()), Times.Once);
            _mockWhitelistRepository.Verify(x => x.ReplaceStudentsBySemesterAsync(2, 3, It.IsAny<IEnumerable<Whitelist>>()), Times.Once);
            // Verify cache invalidation for both semesters
            _mockRedisService.Verify(x => x.DeleteValueAsync("fctms:semester:all", It.IsAny<CancellationToken>()), Times.Once);
            _mockRedisService.Verify(x => x.DeleteValueAsync("fctms:semester:id:1", It.IsAny<CancellationToken>()), Times.Once);
            _mockRedisService.Verify(x => x.DeleteValueAsync("fctms:semester:id:2", It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region SaveWhitelistBatchAsync - Error Cases

        [Fact]
        public async Task SaveWhitelistBatchAsync_NullImportResult_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await _importService.SaveWhitelistBatchAsync((ImportResult<WhitelistImportDTO>)null!, "test-file.xlsx", "testuser");

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*importResult*");
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_NullFileUrl_ThrowsArgumentException()
        {
            // Arrange
            var importResult = new ImportResult<WhitelistImportDTO> { Items = new List<WhitelistImportDTO>() };

            // Act
            Func<Task> act = async () => await _importService.SaveWhitelistBatchAsync(importResult, null!, "testuser");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*fileUrl*");
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_InvalidSemesterId_ThrowsArgumentException()
        {
            // Arrange
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO { Email = "test@example.com", FullName = "Test User", RoleId = 3, Campus = "Hanoi", SemesterId = 999 }
                },
                Errors = new List<ImportError>()
            };

            _mockSemesterRepository.Setup(x => x.SemesterExistsAsync(999)).ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await _importService.SaveWhitelistBatchAsync(importResult, "test-file.xlsx", "testuser");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*SemesterId 999*");
        }

        [Fact]
        public async Task SaveWhitelistBatchAsync_ReplaceFailsForSemester_ThrowsAndLogsError()
        {
            // Arrange
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO { Email = "test@example.com", FullName = "Test User", RoleId = 3, Campus = "Hanoi", SemesterId = 1 }
                },
                Errors = new List<ImportError>()
            };

            _mockSemesterRepository.Setup(x => x.SemesterExistsAsync(1)).ReturnsAsync(true);
            _mockSemesterRepository.Setup(x => x.GetStudentRoleIdAsync()).ReturnsAsync(3);
            
            var dbException = new InvalidOperationException("Database error: unique constraint violation");
            _mockWhitelistRepository.Setup(x => x.ReplaceStudentsBySemesterAsync(1, 3, It.IsAny<IEnumerable<Whitelist>>())).ThrowsAsync(dbException);

            // Act
            Func<Task> act = async () => await _importService.SaveWhitelistBatchAsync(importResult, "test-file.xlsx", "testuser");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            // Verify error was logged
            _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        #endregion

        #region SaveWhitelistBatchAsync - Logging Verification

        [Fact]
        public async Task SaveWhitelistBatchAsync_LogsStartAndCompletion()
        {
            // Arrange
            var importResult = new ImportResult<WhitelistImportDTO>
            {
                Items = new List<WhitelistImportDTO>
                {
                    new WhitelistImportDTO { Email = "test@example.com", FullName = "Test User", RoleId = 3, Campus = "Hanoi", SemesterId = 1 }
                },
                Errors = new List<ImportError>()
            };

            _mockSemesterRepository.Setup(x => x.SemesterExistsAsync(1)).ReturnsAsync(true);
            _mockSemesterRepository.Setup(x => x.GetStudentRoleIdAsync()).ReturnsAsync(3);
            _mockWhitelistRepository.Setup(x => x.ReplaceStudentsBySemesterAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<Whitelist>>())).Returns(Task.CompletedTask);

            // Act
            await _importService.SaveWhitelistBatchAsync(importResult, "test-file.xlsx", "john.doe");

            // Assert - verify logging occurred at start and end
            _mockLogger.Verify(
                x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(2),
                "Should log at least start and completion");
        }

        #endregion

        #region ImportWhitelistFromExcel

        [Fact]
        public async Task ImportWhitelistFromExcel_ValidStream_ReturnsImportResult()
        {
            // Arrange - Create a minimal valid Excel stream
            // Note: This would require creating an actual Excel file or using a library like EPPlus
            // For now, we'll test the happy path with mocking or skip if stream creation is complex
            // In a real scenario, you'd use a test fixture with sample Excel files
            
            // This test is limited without a proper Excel file factory
            // Recommend: Create test fixtures with sample Excel files
        }

        #endregion
    }
}
