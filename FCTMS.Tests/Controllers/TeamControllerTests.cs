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
        public async Task CreateTeam_MissingUserClaim_ReturnsUnauthorized()
        {
            // Act
            // Simulate missing NameIdentifier claim
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); 

            // Act
            var result = await _controller.CreateTeam(new CreateTeamDTO());

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
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
            notFoundResult.Value!.ToString().Should().Contain("Team not found");
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
        [Fact]
        public async Task LeaveTeam_LeaderAttemptingToLeave_ReturnsBadRequest()
        {
            // Arrange
            int teamId = 1;
            int userId = 1; // From constructor claims
            var team = new TeamDTO { TeamId = teamId, LeaderId = userId };

            _mockTeamService.Setup(x => x.GetTeamByIdAsync(teamId, userId))
                .ReturnsAsync(team);

            // Act
            var result = await _controller.LeaveTeam(teamId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value!.ToString().Should().Contain("You are the team leader");
        }

        [Fact]
        public async Task LeaveTeam_MemberLeaving_ReturnsOk()
        {
            // Arrange
            int teamId = 1;
            int userId = 1; // From constructor claims
            int leaderId = 2;
            var team = new TeamDTO { TeamId = teamId, LeaderId = leaderId };

            _mockTeamService.Setup(x => x.GetTeamByIdAsync(teamId, userId))
                .ReturnsAsync(team);
            _mockTeamService.Setup(x => x.RemoveMemberAsync(teamId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.LeaveTeam(teamId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value!.ToString().Should().Contain("Left team successfully");
        }

        [Fact]
        public async Task LeaveTeam_ServiceFails_ReturnsNotFound()
        {
            // Arrange
            int teamId = 1;
            int userId = 1;
            
            // Case where GetTeamById returns null (or fails) OR RemoveMember returns false
            _mockTeamService.Setup(x => x.GetTeamByIdAsync(teamId, userId))
                .ReturnsAsync((TeamDTO)null!);
            _mockTeamService.Setup(x => x.RemoveMemberAsync(teamId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.LeaveTeam(teamId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ChangeLeader_ValidRequest_ReturnsOk()
        {
            // Arrange
            int teamId = 1;
            int currentLeaderId = 1; // From constructor claims
            var dto = new ChangeLeaderDTO { NewLeaderId = 2 };

            _mockTeamService.Setup(x => x.ChangeLeaderAsync(teamId, currentLeaderId, dto.NewLeaderId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangeLeader(teamId, dto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.ToString().Should().Contain("Leadership transferred successfully");
        }

        [Fact]
        public async Task ChangeLeader_NotCurrentLeader_ReturnsForbidden()
        {
            // Arrange
            int teamId = 1;
            int currentLeaderId = 1;
            var dto = new ChangeLeaderDTO { NewLeaderId = 2 };

            _mockTeamService.Setup(x => x.ChangeLeaderAsync(teamId, currentLeaderId, dto.NewLeaderId))
                .ThrowsAsync(new UnauthorizedAccessException("Only the current team leader can transfer leadership."));

            // Act
            var result = await _controller.ChangeLeader(teamId, dto);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(403);
            statusCodeResult.Value.ToString().Should().Contain("Only the current team leader can transfer leadership");
        }

        [Fact]
        public async Task ChangeLeader_NewLeaderNotMember_ReturnsBadRequest()
        {
            // Arrange
            int teamId = 1;
            int currentLeaderId = 1;
            var dto = new ChangeLeaderDTO { NewLeaderId = 99 };

            _mockTeamService.Setup(x => x.ChangeLeaderAsync(teamId, currentLeaderId, dto.NewLeaderId))
                .ThrowsAsync(new ArgumentException("The new leader must be a member of the team."));

            // Act
            var result = await _controller.ChangeLeader(teamId, dto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.ToString().Should().Contain("The new leader must be a member of the team");
        }

        [Fact]
        public async Task ChangeLeader_TeamNotFound_ReturnsNotFound()
        {
            // Arrange
            int teamId = 99;
            int currentLeaderId = 1;
            var dto = new ChangeLeaderDTO { NewLeaderId = 2 };

            _mockTeamService.Setup(x => x.ChangeLeaderAsync(teamId, currentLeaderId, dto.NewLeaderId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ChangeLeader(teamId, dto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.ToString().Should().Contain("Team not found");
        }
    }

}
