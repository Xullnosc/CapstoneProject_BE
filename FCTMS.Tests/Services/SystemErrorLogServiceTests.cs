using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Moq;
using Repositories;
using Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FCTMS.Tests.Services
{
    public class SystemErrorLogServiceTests
    {
        private readonly Mock<ISystemErrorLogRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly SystemErrorLogService _service;

        public SystemErrorLogServiceTests()
        {
            _mockRepo = new Mock<ISystemErrorLogRepository>();
            _mockMapper = new Mock<IMapper>();
            _service = new SystemErrorLogService(_mockRepo.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task AddLogAsync_ShouldReturnSavedLog_WhenCalled()
        {
            // Arrange
            var dto = new SystemErrorLogDTO { Level = "Error", Message = "Test" };
            var entity = new SystemErrorLog { Level = "Error", Message = "Test" };
            
            _mockMapper.Setup(m => m.Map<SystemErrorLog>(dto)).Returns(entity);
            _mockMapper.Setup(m => m.Map<SystemErrorLogDTO>(entity)).Returns(dto);
            _mockRepo.Setup(r => r.AddLogAsync(It.IsAny<SystemErrorLog>())).ReturnsAsync(entity);

            // Act
            var result = await _service.AddLogAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Error", result.Level);
            _mockRepo.Verify(r => r.AddLogAsync(It.IsAny<SystemErrorLog>()), Times.Once);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldReturnPaginatedLogs_WhenCalled()
        {
            // Arrange
            var entityList = new List<SystemErrorLog> { new SystemErrorLog { Id = 1 } };
            var dtoList = new List<SystemErrorLogDTO> { new SystemErrorLogDTO { Id = 1 } };
            
            _mockRepo.Setup(r => r.GetLogsAsync(1, 10, null)).ReturnsAsync((entityList, 1));
            _mockMapper.Setup(m => m.Map<IEnumerable<SystemErrorLogDTO>>(entityList)).Returns(dtoList);

            // Act
            var (logs, total) = await _service.GetLogsAsync(1, 10);

            // Assert
            Assert.Single(logs);
            Assert.Equal(1, total);
        }
    }
}
