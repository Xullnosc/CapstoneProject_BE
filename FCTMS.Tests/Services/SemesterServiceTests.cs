using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using FluentAssertions;
using Moq;
using Repositories;
using Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FCTMS.Tests.Services
{
    public class SemesterServiceTests
    {
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<IArchivingService> _mockArchivingService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly SemesterService _semesterService;

        public SemesterServiceTests()
        {
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockArchivingService = new Mock<IArchivingService>();
            _mockMapper = new Mock<IMapper>();

            _semesterService = new SemesterService(
                _mockSemesterRepository.Object,
                _mockArchivingService.Object,
                _mockMapper.Object
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
                EndDate = DateTime.UtcNow.AddMonths(4),
                IsActive = true
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
            _mockSemesterRepository.Verify(r => r.CreateSemesterAsync(semester), Times.Once);
        }

        [Fact]
        public async Task CreateSemesterAsync_ShouldThrow_WhenCodeExists()
        {
            // Arrange
            var createDto = new SemesterCreateDTO { SemesterCode = "SP26" };
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

        #endregion

        #region UpdateSemesterAsync

        [Fact]
        public async Task UpdateSemesterAsync_ShouldSucceed_WhenCodeIsUnique()
        {
            // Arrange
            var updateDto = new SemesterCreateDTO { SemesterId = 1, SemesterCode = "SU26" };

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
            var updateDto = new SemesterCreateDTO { SemesterId = 1, SemesterCode = "SP26" };
            
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
            var updateDto = new SemesterCreateDTO { SemesterId = 1, SemesterCode = "SP26" };

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
            var semester = new Semester { SemesterId = id, SemesterCode = "SP26" };
            var semesterDTO = new SemesterDTO { SemesterId = id, SemesterCode = "SP26" };

            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync(semester);
            _mockMapper.Setup(m => m.Map<SemesterDTO>(semester)).Returns(semesterDTO);

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
            _mockMapper.Setup(m => m.Map<SemesterDTO>((Semester?)null)).Returns((SemesterDTO?)null);

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
            var semester = new Semester { SemesterId = id, IsActive = true };

            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(id)).ReturnsAsync(semester);
            _mockSemesterRepository.Setup(r => r.UpdateSemesterAsync(semester)).Returns(Task.CompletedTask);
            _mockArchivingService.Setup(s => s.ArchiveSemesterAsync(id)).Returns(Task.CompletedTask);

            // Act
            await _semesterService.EndSemesterAsync(id);

            // Assert
            semester.IsActive.Should().BeFalse();
            _mockSemesterRepository.Verify(r => r.UpdateSemesterAsync(semester), Times.Once);
            _mockArchivingService.Verify(s => s.ArchiveSemesterAsync(id), Times.Once);
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
            _mockArchivingService.Verify(s => s.ArchiveSemesterAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion
    }
}
