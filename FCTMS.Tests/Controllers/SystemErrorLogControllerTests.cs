using CapstoneProject_BE.Controllers;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FCTMS.Tests.Controllers
{
    public class SystemErrorLogControllerTests
    {
        private readonly Mock<ISystemErrorLogService> _mockService;
        private readonly SystemErrorLogController _controller;

        public SystemErrorLogControllerTests()
        {
            _mockService = new Mock<ISystemErrorLogService>();
            _controller = new SystemErrorLogController(_mockService.Object);
        }

        [Fact]
        public async Task GetLogs_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var logs = new List<SystemErrorLogDTO> { new SystemErrorLogDTO { Id = 1, Level = "Error", Message = "Err" } };
            _mockService.Setup(s => s.GetLogsAsync(1, 10, null)).ReturnsAsync((logs, 1));

            // Act
            var result = await _controller.GetLogs(1, 10, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        
        [Fact]
        public async Task GetLogs_ShouldReturn500_WhenExceptionThrown()
        {
            // Arrange
            _mockService.Setup(s => s.GetLogsAsync(1, 10, null)).ThrowsAsync(new System.Exception("Test exception"));

            // Act
            var result = await _controller.GetLogs(1, 10, null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}
