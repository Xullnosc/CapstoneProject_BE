using BusinessObjects.Models;
using Moq;
using Repositories;
using Services;
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading;

namespace FCTMS.Tests.Services
{
    public class WhitelistServiceTests
    {
        private readonly Mock<IWhitelistRepository> _mockWhitelistRepository;
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly WhitelistService _whitelistService;
        private readonly Mock<ILecturerRepository> _mockLecturerRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ISystemUserCredentialRepository> _mockCredentialRepository;
        private readonly Mock<INotificationService> _mockNotificationService;

        public WhitelistServiceTests()
        {
            _mockLecturerRepository = new Mock<ILecturerRepository>();
            _mockWhitelistRepository = new Mock<IWhitelistRepository>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockCredentialRepository = new Mock<ISystemUserCredentialRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockRedisService = new Mock<IRedisService>();
            _mockRedisService.Setup(x => x.DeleteValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockRedisService.Setup(x => x.RemoveByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _whitelistService = new WhitelistService(
                _mockWhitelistRepository.Object,
                _mockSemesterRepository.Object,
                _mockRedisService.Object,
                _mockLecturerRepository.Object,
                _mockUserRepository.Object,
                _mockCredentialRepository.Object,
                _mockNotificationService.Object);
        }
        [Fact]
        public async Task GetWhitelistByRoleAsync_ShouldReturnEntries_WhenRoleHasStudents()
        {
            // Arrange â€” role 2 = Student
            int roleId = 2;
            var entries = new List<Whitelist>
            {
                new Whitelist { WhitelistId = 1, Email = "s1@fpt.edu.vn", RoleId = roleId },
                new Whitelist { WhitelistId = 2, Email = "s2@fpt.edu.vn", RoleId = roleId }
            };
            _mockWhitelistRepository.Setup(x => x.GetByRoleAsync(roleId)).ReturnsAsync(entries);

            // Act
            var result = await _whitelistService.GetWhitelistByRoleAsync(roleId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(e => e.RoleId == roleId);
        }

        [Fact]
        public async Task GetWhitelistByRoleAsync_ShouldReturnEmpty_WhenNoStudentsForRole()
        {
            // Arrange â€” role 99 has no students
            _mockWhitelistRepository.Setup(x => x.GetByRoleAsync(99))
                .ReturnsAsync(new List<Whitelist>());

            // Act
            var result = await _whitelistService.GetWhitelistByRoleAsync(99);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddStudentToWhitelistAsync_ShouldReturnCreated_WhenValidEntry()
        {
            // Arrange
            var entry = new Whitelist { WhitelistId = 0, Email = "newstudent@fpt.edu.vn", RoleId = 2, SemesterId = 1 };
            _mockSemesterRepository.Setup(x => x.GetSemesterByIdAsync(1)).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockWhitelistRepository.Setup(x => x.GetByEmailAndSemesterAsync(It.IsAny<string>(), 1)).ReturnsAsync((Whitelist?)null);
            _mockWhitelistRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Whitelist?)null);
            _mockWhitelistRepository.Setup(x => x.AddAsync(It.IsAny<Whitelist>())).Returns(Task.CompletedTask);

            // Act
            var result = await _whitelistService.AddStudentToWhitelistAsync(entry);

            // Assert
            result.Should().NotBeNull();
            _mockWhitelistRepository.Verify(x => x.AddAsync(It.IsAny<Whitelist>()), Times.Once);
        }

        [Fact]
        public async Task AddStudentToWhitelistAsync_ShouldUpdate_WhenAlreadyInSemester()
        {
            // Arrange
            var entry = new Whitelist { Email = "exists@fpt.edu.vn", RoleId = 3, SemesterId = 1, FullName = "New Name" };
            var existing = new Whitelist { WhitelistId = 10, Email = "exists@fpt.edu.vn", RoleId = 3, SemesterId = 1, FullName = "Old Name" };
            
            _mockSemesterRepository.Setup(x => x.GetSemesterByIdAsync(1)).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockWhitelistRepository.Setup(x => x.GetByEmailAndSemesterAsync("exists@fpt.edu.vn", 1)).ReturnsAsync(existing);
            _mockWhitelistRepository.Setup(x => x.UpdateAsync(existing)).Returns(Task.CompletedTask);

            // Act
            var result = await _whitelistService.AddStudentToWhitelistAsync(entry);

            // Assert
            result.WhitelistId.Should().Be(10);
            result.FullName.Should().Be("New Name");
            _mockWhitelistRepository.Verify(x => x.UpdateAsync(existing), Times.Once);
            _mockWhitelistRepository.Verify(x => x.AddAsync(It.IsAny<Whitelist>()), Times.Never);
        }

        [Fact]
        public async Task AddStudentToWhitelistAsync_ShouldCreateNew_WhenExistingInDifferentSemester()
        {
            // Arrange
            var entry = new Whitelist { Email = "student@fpt.edu.vn", RoleId = 3, SemesterId = 2 };
            var oldEntry = new Whitelist { WhitelistId = 10, Email = "student@fpt.edu.vn", RoleId = 3, SemesterId = 1, StudentCode = "SE123" };
            
            _mockSemesterRepository.Setup(x => x.GetSemesterByIdAsync(2)).ReturnsAsync(new Semester { SemesterId = 2 });
            _mockWhitelistRepository.Setup(x => x.GetByEmailAndSemesterAsync("student@fpt.edu.vn", 2)).ReturnsAsync((Whitelist?)null);
            _mockWhitelistRepository.Setup(x => x.GetByEmailAsync("student@fpt.edu.vn")).ReturnsAsync(oldEntry);
            _mockWhitelistRepository.Setup(x => x.AddAsync(It.IsAny<Whitelist>())).Returns(Task.CompletedTask);

            // Act
            var result = await _whitelistService.AddStudentToWhitelistAsync(entry);

            // Assert
            result.SemesterId.Should().Be(2);
            result.StudentCode.Should().Be("SE123"); // Inherited from historical
            result.Status.Should().Be("Qualified");
            _mockWhitelistRepository.Verify(x => x.AddAsync(It.IsAny<Whitelist>()), Times.Once);
            _mockWhitelistRepository.Verify(x => x.UpdateAsync(It.IsAny<Whitelist>()), Times.Never);
        }

        [Fact]
        public async Task AddStudentToWhitelistAsync_EmailShouldBePreserved()
        {
            // Arrange
            var entry = new Whitelist { Email = "lecturer@fpt.edu.vn", RoleId = 3, SemesterId = 1 };
            _mockSemesterRepository.Setup(x => x.GetSemesterByIdAsync(1)).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockWhitelistRepository.Setup(x => x.GetByEmailAndSemesterAsync(It.IsAny<string>(), 1)).ReturnsAsync((Whitelist?)null);
            _mockWhitelistRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Whitelist?)null);
            _mockWhitelistRepository.Setup(x => x.AddAsync(It.IsAny<Whitelist>())).Returns(Task.CompletedTask);

            // Act
            var result = await _whitelistService.AddStudentToWhitelistAsync(entry);

            // Assert
            result.Email.Should().Be("lecturer@fpt.edu.vn");
        }

        [Fact]
        public async Task UpdateWhitelistAsync_ShouldCallRepository_WhenValidEntry()
        {
            // Arrange
            var entry = new Whitelist { WhitelistId = 3, Email = "updated@fpt.edu.vn", RoleId = 2, SemesterId = 1 };
            _mockWhitelistRepository.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(entry);
            _mockSemesterRepository.Setup(x => x.GetSemesterByIdAsync(1)).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockWhitelistRepository.Setup(x => x.UpdateAsync(entry)).Returns(Task.CompletedTask);

            // Act
            await _whitelistService.UpdateWhitelistAsync(entry);

            // Assert
            _mockWhitelistRepository.Verify(x => x.UpdateAsync(entry), Times.Once);
        }

        [Fact]
        public async Task UpdateWhitelistAsync_ShouldThrow_WhenRepositoryThrows()
        {
            // Arrange
            var entry = new Whitelist { WhitelistId = 999, SemesterId = 1 };
            _mockWhitelistRepository.Setup(x => x.GetByIdAsync(999)).ReturnsAsync(entry);
            _mockSemesterRepository.Setup(x => x.GetSemesterByIdAsync(1)).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockWhitelistRepository.Setup(x => x.UpdateAsync(entry))
                .ThrowsAsync(new KeyNotFoundException("Whitelist not found"));

            // Act
            Func<Task> act = async () => await _whitelistService.UpdateWhitelistAsync(entry);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeleteWhitelistAsync_ShouldCallRepository_WhenValidId()
        {
            // Arrange
            int id = 7;
            var entity = new Whitelist { WhitelistId = id };
            _mockWhitelistRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _mockWhitelistRepository.Setup(x => x.DeleteAsync(entity)).Returns(Task.CompletedTask);

            // Act
            await _whitelistService.DeleteWhitelistAsync(id);

            // Assert
            _mockWhitelistRepository.Verify(x => x.DeleteAsync(entity), Times.Once);
        }

        [Fact]
        public async Task DeleteWhitelistAsync_ShouldNotThrow_WhenEntryNotFound()
        {
            // Arrange
            _mockWhitelistRepository.Setup(x => x.GetByIdAsync(404)).ReturnsAsync((Whitelist?)null);

            // Act
            Func<Task> act = async () => await _whitelistService.DeleteWhitelistAsync(404);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DeleteWhitelistAsync_ShouldNotThrow_WhenSuccessful()
        {
            // Arrange
            int id = 1;
            var entity = new Whitelist { WhitelistId = id };
            _mockWhitelistRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _mockWhitelistRepository.Setup(x => x.DeleteAsync(entity)).Returns(Task.CompletedTask);

            // Act
            Func<Task> act = async () => await _whitelistService.DeleteWhitelistAsync(id);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task GetWhitelistByRoleAsync_DifferentRoles_CallsRepositoryOnce(int roleId)
        {
            // Arrange
            _mockWhitelistRepository.Setup(x => x.GetByRoleAsync(roleId))
                .ReturnsAsync(new List<Whitelist>());

            // Act
            await _whitelistService.GetWhitelistByRoleAsync(roleId);

            // Assert
            _mockWhitelistRepository.Verify(x => x.GetByRoleAsync(roleId), Times.Once);
        }
    }
}

