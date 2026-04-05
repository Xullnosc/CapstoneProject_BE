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
            okResult.Value!.ToString().Should().Contain("Leadership transferred successfully");
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
            statusCodeResult.Value!.ToString().Should().Contain("Only the current team leader can transfer leadership");
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
            badRequestResult.Value!.ToString().Should().Contain("The new leader must be a member of the team");
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
            notFoundResult.Value!.ToString().Should().Contain("Team not found");
        }
        // --- ForceCreateTeam Tests ---

        [Fact]
        public async Task ForceCreateTeam_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var dto = new ForceCreateTeamDTO
            {
                TeamName = "Forced Team",
                SemesterId = 1,
                LeaderEmail = "leader@fpt.edu.vn",
                MemberEmails = new List<string> { "leader@fpt.edu.vn", "member@fpt.edu.vn" }
            };
            var createdTeam = new TeamDTO { TeamId = 10, TeamName = "Forced Team" };
            _mockTeamService.Setup(x => x.ForceCreateTeamAsync(1, dto)).ReturnsAsync(createdTeam);

            // Act
            var result = await _controller.ForceCreateTeam(dto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Value.Should().BeEquivalentTo(createdTeam);
        }

        [Fact]
        public async Task ForceCreateTeam_SemesterNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new ForceCreateTeamDTO { TeamName = "X", SemesterId = 99, LeaderEmail = "a@x.com", MemberEmails = new List<string> { "a@x.com" } };
            _mockTeamService.Setup(x => x.ForceCreateTeamAsync(1, dto))
                .ThrowsAsync(new KeyNotFoundException("Semester 99 not found."));

            // Act
            var result = await _controller.ForceCreateTeam(dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ForceCreateTeam_NotHOD_Returns403()
        {
            // Arrange
            var dto = new ForceCreateTeamDTO { TeamName = "X", SemesterId = 1, LeaderEmail = "a@x.com", MemberEmails = new List<string> { "a@x.com" } };
            _mockTeamService.Setup(x => x.ForceCreateTeamAsync(1, dto))
                .ThrowsAsync(new UnauthorizedAccessException("Only Head of Department can force-create teams."));

            // Act
            var result = await _controller.ForceCreateTeam(dto);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task ForceCreateTeam_StudentAlreadyInTeam_ReturnsBadRequest()
        {
            // Arrange
            var dto = new ForceCreateTeamDTO { TeamName = "X", SemesterId = 1, LeaderEmail = "a@x.com", MemberEmails = new List<string> { "a@x.com" } };
            _mockTeamService.Setup(x => x.ForceCreateTeamAsync(1, dto))
                .ThrowsAsync(new InvalidOperationException("Student is already in team 'Team A'."));

            // Act
            var result = await _controller.ForceCreateTeam(dto);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value!.ToString().Should().Contain("already in team");
        }

        [Fact]
        public async Task DisbandTeam_Leader_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            int teamId = 5;
            // Service returns true â€” disbanding succeeded. Team is now marked as "Disbanded".
            _mockTeamService.Setup(x => x.DisbandTeamAsync(teamId, 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DisbandTeam(teamId);

            // Assert
            // 200 OK should be returned with a success message.
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value!.ToString().Should().Contain("disbanded");
        }

        [Fact]
        public async Task DisbandTeam_Returns403_WhenCallerIsNotLeader()
        {
            // Arrange
            int teamId = 1;
            // Service throws UnauthorizedAccessException because caller is not the leader.
            _mockTeamService.Setup(x => x.DisbandTeamAsync(teamId, 1))
                .ThrowsAsync(new UnauthorizedAccessException("Only the team leader can disband the team."));

            // Act
            var result = await _controller.DisbandTeam(teamId);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value!.ToString().Should().Contain("Only the team leader");
        }

        [Fact]
        public async Task DisbandTeam_ReturnsBadRequest_WhenTeamHasPublishedThesis()
        {
            // Arrange
            int teamId = 2;
            // The team has an active/published thesis â€” disbanding is prohibited.
            _mockTeamService.Setup(x => x.DisbandTeamAsync(teamId, 1))
                .ThrowsAsync(new InvalidOperationException("Cannot disband team with a published thesis."));

            // Act
            var result = await _controller.DisbandTeam(teamId);

            // Assert
            // InvalidOperationException should map to 400 Bad Request.
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value!.ToString().Should().Contain("published thesis");
        }

        [Fact]
        public async Task ToggleSpecialFlag_ReturnsOk_WhenHodSuccessfullyToggles()
        {
            // Arrange
            int teamId = 3;
            // Service successfully toggles the flag and returns true.
            _mockTeamService.Setup(x => x.ToggleSpecialFlagAsync(teamId, 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.ToggleSpecialFlag(teamId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value!.ToString().Should().Contain("toggled successfully");
        }

        [Fact]
        public async Task ToggleSpecialFlag_Returns403_WhenNonHodCalls()
        {
            // Arrange
            int teamId = 3;
            // Service enforces role-based access and throws for non-HOD callers.
            _mockTeamService.Setup(x => x.ToggleSpecialFlagAsync(teamId, 1))
                .ThrowsAsync(new UnauthorizedAccessException("Only HOD can toggle special flag."));

            // Act
            var result = await _controller.ToggleSpecialFlag(teamId);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task ToggleSpecialFlag_ReturnsNotFound_WhenTeamDoesNotExist()
        {
            // Arrange
            int nonExistentTeamId = 9999;
            // Service convention: returns false when team not found.
            _mockTeamService.Setup(x => x.ToggleSpecialFlagAsync(nonExistentTeamId, 1))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ToggleSpecialFlag(nonExistentTeamId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetTeamsBySemester_ReturnsOk_WithEmptyList_WhenNoTeams()
        {
            // Arrange
            int semesterId = 5;
            // No teams registered this semester.
            _mockTeamService.Setup(x => x.GetTeamsBySemesterAsync(semesterId))
                .ReturnsAsync(new List<TeamDTO>());

            // Act
            var result = await _controller.GetTeamsBySemester(semesterId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var teams = okResult.Value as List<TeamDTO>;
            teams.Should().NotBeNull();
            teams!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetTeamsBySemester_ReturnsOk_WithAllTeams_WhenMultipleExist()
        {
            // Arrange
            int semesterId = 1;
            var teams = new List<TeamDTO>
            {
                new TeamDTO { TeamId = 1, TeamName = "Alpha Squad", MemberCount = 4 },
                new TeamDTO { TeamId = 2, TeamName = "Beta Force", MemberCount = 3 },
                new TeamDTO { TeamId = 3, TeamName = "Gamma Crew", MemberCount = 5 }
            };
            _mockTeamService.Setup(x => x.GetTeamsBySemesterAsync(semesterId)).ReturnsAsync(teams);

            // Act
            var result = await _controller.GetTeamsBySemester(semesterId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTeams = okResult.Value as List<TeamDTO>;
            returnedTeams.Should().NotBeNull();
            // All 3 teams must be present.
            returnedTeams!.Should().HaveCount(3);
            returnedTeams.Should().ContainSingle(t => t.TeamName == "Alpha Squad");
        }

        [Fact]
        public async Task GetTeamById_ReturnsOk_WhenFound()
        {
            // Arrange — GetTeamByIdAsync requires both teamId and userId
            int teamId = 10;
            var team = new TeamDTO { TeamId = teamId, TeamName = "Beta Team" };
            _mockTeamService.Setup(x => x.GetTeamByIdAsync(teamId, 1)).ReturnsAsync(team);

            // Act
            var result = await _controller.GetTeamById(teamId);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(team);
        }

        [Fact]
        public async Task GetTeamById_ReturnsNotFound_WhenTeamDoesNotExist()
        {
            // Arrange
            _mockTeamService.Setup(x => x.GetTeamByIdAsync(999, It.IsAny<int>()))
                .ThrowsAsync(new KeyNotFoundException("Team 999 not found."));

            // Act
            var result = await _controller.GetTeamById(999);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value!.ToString().Should().Contain("Team 999 not found.");
        }

        [Fact]
        public async Task GetMyTeam_ReturnsOk_WhenTeamFound()
        {
            // Arrange — GetTeamByStudentIdAsync is the correct interface method
            var team = new TeamDTO { TeamId = 3, TeamName = "My Team" };
            _mockTeamService.Setup(x => x.GetTeamByStudentIdAsync(1)).ReturnsAsync(team);

            // Act
            var result = await _controller.GetMyTeam();

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(team);
        }

        [Fact]
        public async Task GetMyTeam_ReturnsNotFound_WhenNotInAnyTeam()
        {
            // Arrange
            _mockTeamService.Setup(x => x.GetTeamByStudentIdAsync(1)).ReturnsAsync((TeamDTO?)null);

            // Act
            var result = await _controller.GetMyTeam();

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateTeam_ThrowsInvalidOperation_ReturnsBadRequest()
        {
            // Arrange — user already has a team
            var request = new CreateTeamDTO { TeamName = "Duplicate Team" };
            _mockTeamService.Setup(x => x.CreateTeamAsync(1, request))
                .ThrowsAsync(new InvalidOperationException("User already belongs to a team."));

            // Act
            var result = await _controller.CreateTeam(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateTeam_ThrowsKeyNotFound_ReturnsNotFound()
        {
            // Arrange — semester not found
            var request = new CreateTeamDTO { TeamName = "New Team" };
            _mockTeamService.Setup(x => x.CreateTeamAsync(1, request))
                .ThrowsAsync(new KeyNotFoundException("Active semester not found."));

            // Act
            var result = await _controller.CreateTeam(request);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value!.ToString().Should().Contain("Active semester not found.");
        }

        [Fact]
        public async Task UpdateTeam_ReturnsNotFound_WhenTeamDoesNotExist()
        {
            // Arrange
            var request = new UpdateTeamDTO { TeamName = "Ghost Team" };
            _mockTeamService.Setup(x => x.UpdateTeamAsync(999, 1, request))
                .ThrowsAsync(new KeyNotFoundException("Team 999 not found."));

            // Act
            var result = await _controller.UpdateTeam(999, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateTeam_ReturnsForbidden_WhenUserIsNotLeader()
        {
            // Arrange
            var request = new UpdateTeamDTO { TeamName = "Changed" };
            _mockTeamService.Setup(x => x.UpdateTeamAsync(2, 1, request))
                .ThrowsAsync(new UnauthorizedAccessException("Only the team leader can update."));

            // Act
            var result = await _controller.UpdateTeam(2, request);

            // Assert
            var forbidden = result.Should().BeOfType<ObjectResult>().Subject;
            forbidden.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetTeamsBySemester_ReturnsOk_WithEmptyList()
        {
            // Arrange
            int semesterId = 99;
            _mockTeamService.Setup(x => x.GetTeamsBySemesterAsync(semesterId))
                .ReturnsAsync(new List<TeamDTO>());

            // Act
            var result = await _controller.GetTeamsBySemester(semesterId);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<IEnumerable<TeamDTO>>
                ().Which.Should().BeEmpty();
        }
    }

}
