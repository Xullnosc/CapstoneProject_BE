using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using FluentAssertions;
using Moq;
using Repositories;
using Services;
using BusinessObjects.Interfaces;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace FCTMS.Tests.Services
{
    public class SemesterServiceTests
    {
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _mockConfiguration;
        private readonly Mock<ILecturerRepository> _mockLecturerRepository;
        private readonly Mock<IWhitelistRepository> _mockWhitelistRepository;
        private readonly Mock<ICampusContextService> _mockCampusContextService;
        private readonly SemesterService _semesterService;

        public SemesterServiceTests()
        {
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockRedisService = new Mock<IRedisService>();
            _mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _mockLecturerRepository = new Mock<ILecturerRepository>();
            _mockWhitelistRepository = new Mock<IWhitelistRepository>();
            _mockCampusContextService = new Mock<ICampusContextService>();
            _mockCampusContextService.Setup(c => c.GetCurrentCampusId()).Returns(1);

            // Default redis mock behaviors
            _mockRedisService.Setup(r => r.GetObjectAsync<List<SemesterDTO>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<SemesterDTO>?)null);
            _mockRedisService.Setup(r => r.GetObjectAsync<SemesterDTO>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SemesterDTO?)null);
            _mockRedisService.Setup(r => r.SetObjectAsync<List<SemesterDTO>>(It.IsAny<string>(), It.IsAny<List<SemesterDTO>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockRedisService.Setup(r => r.SetObjectAsync<SemesterDTO>(It.IsAny<string>(), It.IsAny<SemesterDTO>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockRedisService.Setup(r => r.DeleteValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Mock roles and whitelists to prevent null references when merging global lecturers
            var roles = new List<Role> { new Role { RoleId = 1, RoleName = "Lecturer" } };
            _mockSemesterRepository.Setup(r => r.GetAllRolesAsync()).ReturnsAsync(roles);
            _mockWhitelistRepository.Setup(w => w.GetByRoleAsync(1)).ReturnsAsync(new List<Whitelist>());

            // Default configuration TTL
            _mockConfiguration.SetupGet(c => c["RedisSettings:SemesterTTLMinutes"]).Returns("30");

            _semesterService = new SemesterService(
                _mockSemesterRepository.Object,
                _mockMapper.Object,
                _mockUserRepository.Object,
                _mockRedisService.Object,
                _mockConfiguration.Object,
                _mockLecturerRepository.Object,
                _mockWhitelistRepository.Object,
                _mockCampusContextService.Object
            );
        }

        #region CreateSemesterAsync

        [Fact]
        public async Task CreateSemesterAsync_ShouldSucceed_WhenCodeIsUnique()
        {
            // Arrange
            var createDto = new SemesterCreateDTO
            {
                SemesterCode = "SP26",
                SemesterName = "Spring 2026",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(4)
            };

            var semester = new Semester
            {
                SemesterCode = "SP26",
                SemesterName = "Spring 2026"
            };

            var createdSemester = new Semester
            {
                SemesterId = 1,
                SemesterCode = "SP26",
                SemesterName = "Spring 2026"
            };

            var resultDto = new SemesterDTO
            {
                SemesterId = 1,
                SemesterCode = "SP26",
                SemesterName = "Spring 2026"
            };

            // Setup: GetSemesterByCodeAsync returns null (code is unique)
            _mockSemesterRepository.Setup(r => r.GetSemesterByCodeAsync(createDto.SemesterCode))
                .ReturnsAsync((Semester?)null);

            _mockMapper.Setup(m => m.Map<Semester>(createDto)).Returns(semester);
            
            _mockSemesterRepository.Setup(r => r.CreateSemesterAsync(semester))
                .ReturnsAsync(createdSemester);

            _mockMapper.Setup(m => m.Map<SemesterDTO>(createdSemester)).Returns(resultDto);

            // Act
            var result = await _semesterService.CreateSemesterAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.SemesterCode.Should().Be("SP26");
            _mockSemesterRepository.Verify(r => r.CreateSemesterAsync(It.Is<Semester>(s => s.Status == "Upcoming")), Times.Once);
        }

        [Fact]
        public async Task CreateSemesterAsync_ShouldThrow_WhenCodeExists()
        {
            // Arrange
            var createDto = new SemesterCreateDTO { SemesterCode = "SP26", SemesterName = "Spring 2026" };
            var existingSemester = new Semester { SemesterId = 99, SemesterCode = "SP26" };

            // Setup: GetSemesterByCodeAsync returns an existing semester
            _mockSemesterRepository.Setup(r => r.GetSemesterByCodeAsync(createDto.SemesterCode))
                .ReturnsAsync(existingSemester);

            // Act
            Func<Task> act = async () => await _semesterService.CreateSemesterAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Semester code '{createDto.SemesterCode}' already exists.");
            
            _mockSemesterRepository.Verify(r => r.CreateSemesterAsync(It.IsAny<Semester>()), Times.Never);
        }

        [Fact]
        public async Task CreateSemesterAsync_ShouldThrow_WhenDatesOverlap()
        {
            // Arrange
            var createDto = new SemesterCreateDTO 
            { 
                SemesterCode = "SP26", 
                SemesterName = "Spring 2026",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(4)
            };

            var conflictSemester = new Semester { SemesterCode = "SU26", SemesterName = "Summer 2026" };

            // Setup: IsOverlapAsync returns conflict semester
            _mockSemesterRepository.Setup(r => r.IsOverlapAsync(createDto.StartDate, createDto.EndDate, null))
                .ReturnsAsync(conflictSemester);

            // Act
            Func<Task> act = async () => await _semesterService.CreateSemesterAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Semester dates overlap with another existing semester: 'Summer 2026' (SU26).");
            
            _mockSemesterRepository.Verify(r => r.CreateSemesterAsync(It.IsAny<Semester>()), Times.Never);
        }

        #endregion

        #region UpdateSemesterAsync

        [Fact]
        public async Task UpdateSemesterAsync_ShouldSucceed_WhenCodeIsUnique()
        {
            // Arrange
            var updateDto = new SemesterCreateDTO { SemesterId = 1, SemesterCode = "SU26", SemesterName = "Summer 2026" };

            // Setup: GetSemesterByCodeAsync returns null
            _mockSemesterRepository.Setup(r => r.GetSemesterByCodeAsync(updateDto.SemesterCode))
                .ReturnsAsync((Semester?)null);

            var semesterToUpdate = new Semester { SemesterId = 1, SemesterCode = "SU26" };
            _mockMapper.Setup(m => m.Map<Semester>(updateDto)).Returns(semesterToUpdate);

            // Act
            await _semesterService.UpdateSemesterAsync(updateDto);

            // Assert
            _mockSemesterRepository.Verify(r => r.UpdateSemesterAsync(semesterToUpdate), Times.Once);
        }

        [Fact]
        public async Task UpdateSemesterAsync_ShouldThrow_WhenCodeExistsAndNotSameId()
        {
            // Arrange
            var updateDto = new SemesterCreateDTO { SemesterId = 1, SemesterCode = "SP26", SemesterName = "Spring 2026" };
            
            // Existing semester with same code but DIFFERENT ID
            var conflictSemester = new Semester { SemesterId = 2, SemesterCode = "SP26" };

            _mockSemesterRepository.Setup(r => r.GetSemesterByCodeAsync(updateDto.SemesterCode))
                .ReturnsAsync(conflictSemester);

            // Act
            Func<Task> act = async () => await _semesterService.UpdateSemesterAsync(updateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Semester code '{updateDto.SemesterCode}' already exists.");

            _mockSemesterRepository.Verify(r => r.UpdateSemesterAsync(It.IsAny<Semester>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSemesterAsync_ShouldSucceed_WhenCodeExistsButIsSameId()
        {
            // Arrange
            var updateDto = new SemesterCreateDTO { SemesterId = 1, SemesterCode = "SP26", SemesterName = "Spring 2026" };

            // Existing semester is SELF (same ID)
            var selfSemester = new Semester { SemesterId = 1, SemesterCode = "SP26" };

            _mockSemesterRepository.Setup(r => r.GetSemesterByCodeAsync(updateDto.SemesterCode))
                .ReturnsAsync(selfSemester);

            var semesterToUpdate = new Semester { SemesterId = 1, SemesterCode = "SP26" };
            _mockMapper.Setup(m => m.Map<Semester>(updateDto)).Returns(semesterToUpdate);

            // Act
            await _semesterService.UpdateSemesterAsync(updateDto);

            // Assert
            _mockSemesterRepository.Verify(r => r.UpdateSemesterAsync(semesterToUpdate), Times.Once);
        }

        #endregion

        #region GetAllSemestersAsync

        [Fact]
        public async Task GetAllSemestersAsync_ShouldReturnList_WhenSemestersExist()
        {
            // Arrange
            var semesters = new List<Semester>
            {
                new Semester { SemesterId = 1, SemesterCode = "SP26" },
                new Semester { SemesterId = 2, SemesterCode = "SU26" }
            };
            var semesterDTOs = new List<SemesterDTO>
            {
                new SemesterDTO { SemesterId = 1, SemesterCode = "SP26" },
                new SemesterDTO { SemesterId = 2, SemesterCode = "SU26" }
            };

            _mockSemesterRepository.Setup(r => r.GetAllSemestersAsync()).ReturnsAsync(semesters);
            _mockMapper.Setup(m => m.Map<List<SemesterDTO>>(semesters)).Returns(semesterDTOs);

            // Act
            var result = await _semesterService.GetAllSemestersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result[0].SemesterCode.Should().Be("SP26");
        }

        [Fact]
        public async Task GetAllSemestersAsync_ShouldReturnEmptyList_WhenNoSemestersExist()
        {
            // Arrange
            var semesters = new List<Semester>();
            var semesterDTOs = new List<SemesterDTO>();

            _mockSemesterRepository.Setup(r => r.GetAllSemestersAsync()).ReturnsAsync(semesters);
            _mockMapper.Setup(m => m.Map<List<SemesterDTO>>(semesters)).Returns(semesterDTOs);

            // Act
            var result = await _semesterService.GetAllSemestersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetSemesterByIdAsync

        [Fact]
        public async Task GetSemesterByIdAsync_ShouldReturnSemester_WhenFound()
        {
            // Arrange
            int id = 1;
            var semester = new Semester
            {
                SemesterId = id,
                SemesterCode = "SP26",
                Teams = new List<Team>(),
                Whitelists = new List<Whitelist>()
            };
            var semesterDTO = new SemesterDTO
            {
                SemesterId = id,
                SemesterCode = "SP26",
                Teams = new List<TeamSimpleDTO>(),
                Whitelists = new List<WhitelistDTO>()
            };

            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync(semester);
            _mockMapper.Setup(m => m.Map<SemesterDTO>(semester)).Returns(semesterDTO);

            // Mock studentRoleId
            _mockSemesterRepository.Setup(r => r.GetStudentRoleIdAsync()).ReturnsAsync(2);

            // Mock mapper for list types used inside the method
            _mockMapper.Setup(m => m.Map<List<WhitelistDTO>>(It.IsAny<List<Whitelist>>()))
                .Returns(new List<WhitelistDTO>());

            // Mock user lookup for avatar population
            _mockUserRepository.Setup(u => u.GetUsersByEmailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _semesterService.GetSemesterByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.SemesterCode.Should().Be("SP26");
        }

        [Fact]
        public async Task GetSemesterByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            int id = 99;
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync((Semester?)null);

            // Act
            var result = await _semesterService.GetSemesterByIdAsync(id);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region EndSemesterAsync

        [Fact]
        public async Task EndSemesterAsync_ShouldSucceed_WhenIdExists()
        {
            // Arrange
            int id = 1;
            var semester = new Semester { SemesterId = id, Status = "Active" };

            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync(semester);
            _mockSemesterRepository.Setup(r => r.UpdateSemesterAsync(semester)).Returns(Task.CompletedTask);

            // Act
            await _semesterService.EndSemesterAsync(id);

            // Assert
            semester.Status.Should().Be("Ended");
            _mockSemesterRepository.Verify(r => r.UpdateSemesterAsync(semester), Times.Once);
        }


        [Fact]
        public async Task EndSemesterAsync_ShouldThrowKeyNotFound_WhenIdDoesNotExist()
        {
             // Arrange
            int id = 99;
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync((Semester?)null);

            // Act
            Func<Task> act = async () => await _semesterService.EndSemesterAsync(id);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Semester with ID {id} not found.");
            
            _mockSemesterRepository.Verify(r => r.UpdateSemesterAsync(It.IsAny<Semester>()), Times.Never);
        }

        #endregion

        #region GetOrphanedStudentsAsync

        [Fact]
        public async Task GetOrphanedStudentsAsync_ShouldThrow_WhenSemesterNotFound()
        {
            // Arrange
            int id = 99;
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync((Semester?)null);

            // Act
            Func<Task> act = async () => await _semesterService.GetOrphanedStudentsAsync(id);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Semester 99 not found");
        }

        [Fact]
        public async Task GetOrphanedStudentsAsync_ShouldReturnOrphanedStudents()
        {
            // Arrange
            int id = 1;
            var semester = new Semester { SemesterId = id };
            var orphanedWhitelists = new List<Whitelist>
            {
                new Whitelist { WhitelistId = 1, Email = "orphan@fpt.edu.vn", FullName = "Orphan" }
            };
            var orphanedDTOs = new List<WhitelistDTO>
            {
                new WhitelistDTO { WhitelistId = 1, Email = "orphan@fpt.edu.vn", FullName = "Orphan" }
            };

            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync(semester);
            _mockSemesterRepository.Setup(r => r.GetOrphanedStudentsAsync(id)).ReturnsAsync(orphanedWhitelists);
            _mockMapper.Setup(m => m.Map<List<WhitelistDTO>>(orphanedWhitelists)).Returns(orphanedDTOs);
            _mockUserRepository.Setup(u => u.GetUsersByEmailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<User> { new User { Email = "orphan@fpt.edu.vn", Avatar = "avatar.png" } });

            // Act
            var result = await _semesterService.GetOrphanedStudentsAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Email.Should().Be("orphan@fpt.edu.vn");
            result[0].Avatar.Should().Be("avatar.png");
        }

        [Fact]
        public async Task GetOrphanedStudentsAsync_ShouldReturnEmptyList_WhenNoOrphans()
        {
            // Arrange
            int id = 1;
            var semester = new Semester { SemesterId = id };

            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync(semester);
            _mockSemesterRepository.Setup(r => r.GetOrphanedStudentsAsync(id)).ReturnsAsync(new List<Whitelist>());
            _mockMapper.Setup(m => m.Map<List<WhitelistDTO>>(It.IsAny<List<Whitelist>>())).Returns(new List<WhitelistDTO>());

            // Act
            var result = await _semesterService.GetOrphanedStudentsAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion
    }
}
