using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Moq;
using Repositories;
using Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FCTMS.Tests
{
    public class SystemParameterServiceTests
    {
        private readonly Mock<ISystemParameterRepository> _mockRepository;
        private readonly SystemParameterService _service;

        public SystemParameterServiceTests()
        {
            _mockRepository = new Mock<ISystemParameterRepository>();
            _service = new SystemParameterService(_mockRepository.Object);
        }

        [Fact]
        public async Task UpdateParameterAsync_ShouldCallRepositoryUpdate_WhenParameterExists()
        {
            // Arrange
            var key = "MAX_TEAM_SIZE";
            var existingParam = new SystemParameter
            {
                Key = key,
                Value = "5",
                Description = "Old description",
                CreatedAt = DateTime.UtcNow
            };

            var updateDto = new SystemParameterDTO
            {
                Key = key,
                Value = "6",
                Description = "New description"
            };

            _mockRepository.Setup(r => r.GetParameterByKeyAsync(key))
                .ReturnsAsync(existingParam);

            _mockRepository.Setup(r => r.UpdateParameterAsync(It.IsAny<SystemParameter>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateParameterAsync(updateDto);

            // Assert
            _mockRepository.Verify(r => r.GetParameterByKeyAsync(key), Times.Once);
            _mockRepository.Verify(r => r.UpdateParameterAsync(It.Is<SystemParameter>(p => 
                p.Key == key && p.Value == "6" && p.Description == "New description")), Times.Once);
        }

        [Fact]
        public async Task UpdateParameterAsync_ShouldNotCallRepositoryUpdate_WhenParameterDoesNotExist()
        {
            // Arrange
            var key = "UNKNOWN_KEY";
            var updateDto = new SystemParameterDTO
            {
                Key = key,
                Value = "Some value"
            };

            _mockRepository.Setup(r => r.GetParameterByKeyAsync(key))
                .ReturnsAsync((SystemParameter?)null);

            // Act
            await _service.UpdateParameterAsync(updateDto);

            // Assert
            _mockRepository.Verify(r => r.GetParameterByKeyAsync(key), Times.Once);
            _mockRepository.Verify(r => r.UpdateParameterAsync(It.IsAny<SystemParameter>()), Times.Never);
        }
    }
}
