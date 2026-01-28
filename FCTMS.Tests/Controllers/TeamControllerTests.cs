using System.Security.Claims;
using BusinessObjects.DTOs;
using CapstoneProject_BE.Controllers;
using Services.Helpers;
using Services;
using Repositories;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Http;

namespace FCTMS.Tests.Controllers
{
    public class TeamControllerTests
    {
        private readonly Mock<ITeamService> _mockTeamService;
        private readonly TeamController _controller;

        public TeamControllerTests()
        {
            _mockTeamService = new Mock<ITeamService>();
            _controller = new TeamController(_mockTeamService.Object);

            // Mock User (ClaimsPrincipal)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        // --- Normal Cases (Happy Path) ---

        [Fact]
        public async Task CreateTeam_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var request = new CreateTeamDTO { TeamName = "New Team" };
            var createdTeam = new TeamDTO { TeamId = 1, TeamName = "New Team" };

            _mockTeamService.Setup(x => x.CreateTeamAsync(1, request))
                .ReturnsAsync(createdTeam);

            // Act
            var result = await _controller.CreateTeam(request);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Value.Should().BeEquivalentTo(createdTeam);
        }

        [Fact]
        public async Task UpdateTeam_ValidRequest_ReturnsOk()
        {
            // Arrange
            int teamId = 1;
            var request = new UpdateTeamDTO { TeamName = "Updated Team" };
            var updatedTeam = new TeamDTO { TeamId = teamId, TeamName = "Updated Team" };

            _mockTeamService.Setup(x => x.UpdateTeamAsync(teamId, 1, request))
                .ReturnsAsync(updatedTeam);

            // Act
            var result = await _controller.UpdateTeam(teamId, request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(updatedTeam);
        }

        [Fact]
        public async Task GetTeamById_ExistingTeam_ReturnsOk()
        {
            // Arrange
            int teamId = 1;
            var team = new TeamDTO { TeamId = teamId, TeamName = "Team A" };

            _mockTeamService.Setup(x => x.GetTeamByIdAsync(teamId, 1))
                .ReturnsAsync(team);

            // Act
            var result = await _controller.GetTeamById(teamId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(team);
        }

        [Fact]
        public async Task GetTeamsBySemester_ReturnsOk()
        {
            // Arrange
            int semesterId = 1;
            var teams = new List<TeamDTO> { new TeamDTO { TeamId = 1 }, new TeamDTO { TeamId = 2 } };

            _mockTeamService.Setup(x => x.GetTeamsBySemesterAsync(semesterId))
                .ReturnsAsync(teams);

            // Act
            var result = await _controller.GetTeamsBySemester(semesterId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(teams);
        }


        // --- Abnormal Cases (Abnormal & Edge Cases) ---

        [Fact]
        public async Task CreateTeam_InvalidModel_ReturnsBadRequest()
        {
            // Act
            // In a real integration test, ASP.NET Core validation middleware handles this.
            // For unit testing controller, we can simulate service exception if validation logic is there,
            // or simply simulate the generic Exception catch if that's what we want to test.
            // But let's verify scenarios where 'User' claim is missing (parsing error).
            
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

            // Act
            var result = await _controller.CreateTeam(new CreateTeamDTO());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateTeam_DuplicateName_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateTeamDTO { TeamName = "Duplicate Team" };
            _mockTeamService.Setup(x => x.CreateTeamAsync(It.IsAny<int>(), request))
                .ThrowsAsync(new ArgumentException("Team name already exists"));

            // Act
            var result = await _controller.CreateTeam(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "Team name already exists" });
        }

        [Fact]
        public async Task UpdateTeam_NonExistentTeam_ReturnsNotFound()
        {
            // Arrange
            int teamId = 99;
            var request = new UpdateTeamDTO();
            _mockTeamService.Setup(x => x.UpdateTeamAsync(teamId, 1, request))
                .ThrowsAsync(new KeyNotFoundException("Team not found"));

            // Act
            var result = await _controller.UpdateTeam(teamId, request);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeEquivalentTo(new { message = "Team not found" });
        }

        [Fact]
        public async Task DisbandTeam_NonExistentTeam_ReturnsNotFound()
        {
            // Arrange
            int teamId = 99;
            _mockTeamService.Setup(x => x.DisbandTeamAsync(teamId, 1))
                .ReturnsAsync(false); // Service returns false if not found/failed

            // Act
            var result = await _controller.DisbandTeam(teamId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.ToString().Should().Contain("Team not found");
        }

        [Fact]
        public async Task GetTeamById_NonExistent_ReturnsNotFound()
        {
            // Arrange
            int teamId = 99;
            _mockTeamService.Setup(x => x.GetTeamByIdAsync(teamId, 1))
                .ReturnsAsync((TeamDTO)null!);

            // Act
            var result = await _controller.GetTeamById(teamId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateTeam_ServiceError_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateTeamDTO();
            _mockTeamService.Setup(x => x.CreateTeamAsync(It.IsAny<int>(), request))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.CreateTeam(request);

            // Assert
            // Note: TeamController catches Exception and returns BadRequest, NOT 500.
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "Database connection failed" });
        }
    }

}
