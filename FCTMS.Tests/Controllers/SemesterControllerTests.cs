using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using BusinessObjects;
using BusinessObjects.DTOs;
using CapstoneProject_BE.Controllers;

namespace FCTMS.Tests.Controllers
{
    public class SemesterControllerTests
    {
        private readonly Mock<ISemesterService> _mockSemesterService;
        private readonly SemesterController _controller;

        public SemesterControllerTests()
        {
            _mockSemesterService = new Mock<ISemesterService>();
            _controller = new SemesterController(_mockSemesterService.Object);

             // Mock User (ClaimsPrincipal) - Role HOD
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, CampusConstants.Roles.HOD)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        // --- Normal Cases (Happy Path) ---

        [Fact]
        public async Task GetSemesters_ReturnsOk()
        {
            // Arrange
            var semesters = new List<SemesterDTO> 
            { 
                new SemesterDTO { SemesterId = 1, SemesterName = "SP26" } 
            };
            
            _mockSemesterService.Setup(x => x.GetAllSemestersAsync())
                .ReturnsAsync(semesters);

            // Act
            var result = await _controller.GetSemesters();

            // Assert
            result.Value.Should().BeEquivalentTo(semesters);
        }

        [Fact]
        public async Task GetSemester_ExistingId_ReturnsOk()
        {
            // Arrange
            int id = 1;
            var semester = new SemesterDTO { SemesterId = id, SemesterName = "SP26" };

            _mockSemesterService.Setup(x => x.GetSemesterByIdAsync(id))
                .ReturnsAsync(semester);

            // Act
            var result = await _controller.GetSemester(id);

            // Assert
             result.Value.Should().BeEquivalentTo(semester);
        }

        [Fact]
        public async Task CreateSemester_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var request = new SemesterCreateDTO { SemesterName = "SP26"};
            var created = new SemesterDTO { SemesterId = 1, SemesterName = "SP26" };

            _mockSemesterService.Setup(x => x.CreateSemesterAsync(request))
                .ReturnsAsync(created);

            // Act
            var result = await _controller.CreateSemester(request);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Value.Should().BeEquivalentTo(created);
        }

        [Fact]
        public async Task UpdateSemester_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            int id = 1;
            var request = new SemesterCreateDTO { SemesterId = id, SemesterName = "FA26" };

            _mockSemesterService.Setup(x => x.UpdateSemesterAsync(request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateSemester(id, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteSemester_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            int id = 1;
            _mockSemesterService.Setup(x => x.DeleteSemesterAsync(id))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteSemester(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task EndSemester_ValidId_ReturnsOk()
        {
             // Arrange
            int id = 1;
            _mockSemesterService.Setup(x => x.EndSemesterAsync(id))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.EndSemester(id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.ToString().Should().Contain("ended successfully");
        }


        // --- Abnormal Cases (Abnormal & Edge Cases) ---

        [Fact]
        public async Task GetSemester_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            int id = 99;
            _mockSemesterService.Setup(x => x.GetSemesterByIdAsync(id))
                .ReturnsAsync((SemesterDTO)null!);

            // Act
            var result = await _controller.GetSemester(id);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateSemester_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            int id = 1;
            var request = new SemesterCreateDTO { SemesterId = 999 }; // Mismatch ID

            // Act
            var result = await _controller.UpdateSemester(id, request);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task EndSemester_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            int id = 99;
            _mockSemesterService.Setup(x => x.EndSemesterAsync(id))
                .ThrowsAsync(new KeyNotFoundException("Semester not found"));

            // Act
            var result = await _controller.EndSemester(id);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeEquivalentTo(new { message = "Semester not found" });
        }

        [Fact]
        public async Task EndSemester_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            int id = 1;
            _mockSemesterService.Setup(x => x.EndSemesterAsync(id))
                .ThrowsAsync(new Exception("Database corruption"));

            // Act
            var result = await _controller.EndSemester(id);

            // Assert
            var serverError = result.Should().BeOfType<ObjectResult>().Subject;
            serverError.StatusCode.Should().Be(500);
            serverError.Value.ToString().Should().Contain("An error occurred");
        }
    }
}
