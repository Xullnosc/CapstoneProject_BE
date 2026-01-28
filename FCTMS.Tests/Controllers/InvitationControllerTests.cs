using BusinessObjects.DTOs;
using BusinessObjects;
using CapstoneProject_BE.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services;
using System.Security.Claims;

namespace FCTMS.Tests.Controllers
{
    public class InvitationControllerTests
    {
        private readonly Mock<ITeamInvitationService> _mockInvitationService;
        private readonly InvitationController _controller;

        public InvitationControllerTests()
        {
            _mockInvitationService = new Mock<ITeamInvitationService>();
            _controller = new InvitationController(_mockInvitationService.Object);

            // Mock User Claims
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, CampusConstants.Roles.Student)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task GetMyInvitations_ReturnsOk_WithList()
        {
            // Arrange
            var mockList = new List<TeamInvitationDTO> 
            { 
                new TeamInvitationDTO 
                { 
                    InvitationId = 1,
                    Team = new TeamInfoDTO(),
                    InvitedBy = new InvitedByDTO()
                } 
            };
            _mockInvitationService.Setup(s => s.GetMyInvitationsAsync(1))
                .ReturnsAsync(mockList);

            // Act
            var result = await _controller.GetMyInvitations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedList = Assert.IsType<List<TeamInvitationDTO>>(okResult.Value);
            Assert.Single(returnedList);
        }

        [Fact]
        public async Task AcceptInvitation_ReturnsOk_WhenSuccess()
        {
            // Arrange
            _mockInvitationService.Setup(s => s.AcceptInvitationAsync(1, 1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AcceptInvitation(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AcceptInvitation_ReturnsNotFound_WhenKeyNotFound()
        {
             // Arrange
            _mockInvitationService.Setup(s => s.AcceptInvitationAsync(1, 1))
                .ThrowsAsync(new KeyNotFoundException("Not found"));

            // Act
            var result = await _controller.AcceptInvitation(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AcceptInvitation_ReturnsForbidden_WhenUnauthorized()
        {
             // Arrange
            _mockInvitationService.Setup(s => s.AcceptInvitationAsync(1, 1))
                .ThrowsAsync(new UnauthorizedAccessException("Forbidden"));

            // Act
            var result = await _controller.AcceptInvitation(1);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
        }

        [Fact]
        public async Task DeclineInvitation_ReturnsOk_WhenSuccess()
        {
             // Arrange
            _mockInvitationService.Setup(s => s.DeclineInvitationAsync(1, 1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeclineInvitation(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
        
        [Fact]
        public async Task DeclineInvitation_ReturnsBadRequest_WhenInvalidOperation()
        {
             // Arrange
            _mockInvitationService.Setup(s => s.DeclineInvitationAsync(1, 1))
                .ThrowsAsync(new InvalidOperationException("Invalid"));

            // Act
            var result = await _controller.DeclineInvitation(1);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AcceptInvitation_ReturnsBadRequest_WhenInvalidOperation()
        {
             // Arrange
            _mockInvitationService.Setup(s => s.AcceptInvitationAsync(1, 1))
                .ThrowsAsync(new InvalidOperationException("Team is full"));

            // Act
            var result = await _controller.AcceptInvitation(1);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeclineInvitation_ReturnsNotFound_WhenKeyNotFound()
        {
             // Arrange
            _mockInvitationService.Setup(s => s.DeclineInvitationAsync(1, 1))
                .ThrowsAsync(new KeyNotFoundException("Not found"));

            // Act
            var result = await _controller.DeclineInvitation(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeclineInvitation_ReturnsForbidden_WhenUnauthorized()
        {
             // Arrange
            _mockInvitationService.Setup(s => s.DeclineInvitationAsync(1, 1))
                .ThrowsAsync(new UnauthorizedAccessException("Forbidden"));

            // Act
            var result = await _controller.DeclineInvitation(1);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
        }

        [Fact]
        public async Task AcceptInvitation_Returns500_WhenGenericException()
        {
             // Arrange
            _mockInvitationService.Setup(s => s.AcceptInvitationAsync(1, 1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.AcceptInvitation(1);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
