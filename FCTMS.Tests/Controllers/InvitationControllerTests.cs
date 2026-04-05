using BusinessObjects.DTOs;
using BusinessObjects;
using CapstoneProject_BE.Controllers;
using CapstoneProject_BE.DTOs.Requests;
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

        [Fact]
        public async Task GetMyInvitations_ReturnsOk_WithEmptyList_WhenNoInvitations()
        {
            // Arrange
            // Service returns an empty list â€” student has no pending invitations.
            _mockInvitationService.Setup(s => s.GetMyInvitationsAsync(1))
                .ReturnsAsync(new List<TeamInvitationDTO>());

            // Act
            var result = await _controller.GetMyInvitations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedList = Assert.IsType<List<TeamInvitationDTO>>(okResult.Value);
            // The list must be empty, not null.
            Assert.Empty(returnedList);
        }

        [Fact]
        public async Task GetMyInvitations_Returns500_WhenServiceThrowsUnexpectedException()
        {
            // Arrange
            // Simulates a database crash or network timeout during invitation fetch.
            _mockInvitationService.Setup(s => s.GetMyInvitationsAsync(1))
                .ThrowsAsync(new Exception("Connection timeout"));

            // Act
            var result = await _controller.GetMyInvitations();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task SendInvitation_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            int teamId = 1;
            string studentEmail = "student@fpt.edu.vn";
            var request = new SendInvitationRequest { TeamId = teamId, StudentCodeOrEmail = studentEmail };
            var createdInvitation = new TeamInvitationDTO
            {
                InvitationId = 50,
                Team = new TeamInfoDTO { TeamName = "My Team" },
                InvitedBy = new InvitedByDTO()
            };

            // Service creates the invitation successfully.
            _mockInvitationService.Setup(s => s.SendInvitationAsync(teamId, 1, studentEmail))
                .ReturnsAsync(createdInvitation);

            // Act â€” controller takes a single SendInvitationRequest body object.
            var result = await _controller.SendInvitation(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dataProp = okResult.Value!.GetType().GetProperty("data");
            var returnedInvitation = Assert.IsType<TeamInvitationDTO>(dataProp!.GetValue(okResult.Value, null));
            Assert.Equal(50, returnedInvitation.InvitationId);
        }

        [Fact]
        public async Task SendInvitation_ReturnsBadRequest_WhenStudentAlreadyInvited()
        {
            // Arrange
            int teamId = 1;
            string studentEmail = "alreadyInvited@fpt.edu.vn";
            var request = new SendInvitationRequest { TeamId = teamId, StudentCodeOrEmail = studentEmail };
            // Sending to an already-invited student is an invalid operation.
            _mockInvitationService.Setup(s => s.SendInvitationAsync(teamId, 1, studentEmail))
                .ThrowsAsync(new InvalidOperationException("Student is already invited to this team."));

            // Act
            var result = await _controller.SendInvitation(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SendInvitation_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            // Arrange
            int teamId = 1;
            string unknownEmail = "ghost@nowhere.com";
            var request = new SendInvitationRequest { TeamId = teamId, StudentCodeOrEmail = unknownEmail };
            // The student cannot be found in the system â€” invitation cannot be sent.
            _mockInvitationService.Setup(s => s.SendInvitationAsync(teamId, 1, unknownEmail))
                .ThrowsAsync(new KeyNotFoundException("Student not found."));

            // Act
            var result = await _controller.SendInvitation(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CancelInvitation_ReturnsOk_WhenSuccessfullyCancelled()
        {
            // Arrange
            int invitationId = 10;
            // Service completes without throwing â€” successful cancellation.
            _mockInvitationService.Setup(s => s.CancelInvitationAsync(invitationId, 1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CancelInvitation(invitationId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CancelInvitation_ReturnsBadRequest_WhenAlreadyAccepted()
        {
            // Arrange
            int invitationId = 11;
            // Cancelling an accepted invitation is an invalid operation.
            _mockInvitationService.Setup(s => s.CancelInvitationAsync(invitationId, 1))
                .ThrowsAsync(new InvalidOperationException("Cannot cancel an accepted invitation."));

            // Act
            var result = await _controller.CancelInvitation(invitationId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
