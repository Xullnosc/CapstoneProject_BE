using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using FluentAssertions;
using Moq;
using Repositories;
using Services;
using Services.Helpers;
using Xunit;
using BusinessObjects.Interfaces;

namespace FCTMS.Tests.Services
{
    public class TeamServiceTests
    {
        private readonly Mock<ITeamRepository> _mockTeamRepository;
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ICloudinaryHelper> _mockCloudinaryHelper;
        private readonly Mock<ITeamMemberRepository> _mockTeamMemberRepository;
        private readonly Mock<IThesisRepository> _mockThesisRepository;
        private readonly Mock<ISemesterService> _mockSemesterService;
        private readonly Mock<IWhitelistRepository> _mockWhitelistRepository;
        private readonly Mock<ICampusContextService> _mockCampusContextService;
        private readonly TeamService _teamService;

        public TeamServiceTests()
        {
            _mockTeamRepository = new Mock<ITeamRepository>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockCloudinaryHelper = new Mock<ICloudinaryHelper>();
            _mockTeamMemberRepository = new Mock<ITeamMemberRepository>();
            _mockThesisRepository = new Mock<IThesisRepository>();
            _mockSemesterService = new Mock<ISemesterService>();
            _mockWhitelistRepository = new Mock<IWhitelistRepository>();
            _mockCampusContextService = new Mock<ICampusContextService>();
            _mockCampusContextService.Setup(c => c.GetCurrentCampusId()).Returns(1);
            _teamService = new TeamService(
                _mockTeamRepository.Object,
                _mockSemesterRepository.Object,
                _mockUserRepository.Object,
                _mockCloudinaryHelper.Object,
                _mockTeamMemberRepository.Object,
                _mockThesisRepository.Object,
                _mockSemesterService.Object,
                _mockWhitelistRepository.Object,
                _mockCampusContextService.Object
            );
        }

        [Fact]
        public async Task DisbandTeamAsync_UpdatesStatusToDisbanded_WhenLeaderRequests()
        {
            // Arrange
            int teamId = 1;
            int leaderId = 100;
            var team = new Team
            {
                TeamId = teamId,
                LeaderId = leaderId,
                Status = "Insufficient",
                SemesterId = 1,
                Semester = new Semester { Status = "Open" }
            };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(team);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team))
                .ReturnsAsync(true);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync())
                .ReturnsAsync(new Semester { Status = "Open" });
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId))
                .ReturnsAsync(new List<Thesis>());
            _mockTeamMemberRepository.Setup(r => r.RemoveAllMembersFromTeamAsync(teamId))
                .ReturnsAsync(true);

            // Act
            var result = await _teamService.DisbandTeamAsync(teamId, leaderId);
            
            // Assert
            result.Should().BeTrue();
            team.Status.Should().Be("Disbanded");
            _mockTeamRepository.Verify(r => r.UpdateAsync(team), Times.Once);
            _mockTeamMemberRepository.Verify(r => r.RemoveAllMembersFromTeamAsync(teamId), Times.Once);
        }

        [Fact]
        public async Task DisbandTeamAsync_ReturnsFalse_WhenTeamNotFound()
        {
            // Arrange
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Team?)null);
            // Act
            var result = await _teamService.DisbandTeamAsync(1, 1);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_ThrowsException_WhenNotLeader()
        {
            // Arrange
            var team = new Team { TeamId = 1, LeaderId = 100 };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(team);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync())
                .ReturnsAsync(new Semester { Status = "Open" });
            
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _teamService.DisbandTeamAsync(1, 99));
        }

        [Fact]
        public async Task CreateTeamAsync_ShouldSucceed_WhenValid()
        {
            // Arrange
            int leaderId = 1;
            var createDto = new CreateTeamDTO { TeamName = "Team A" };
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP26", Status = "Open" };
            var user = new User { UserId = leaderId, Email = "test@edu.vn" };

            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetByIdAsync(leaderId)).ReturnsAsync(user);
            _mockWhitelistRepository.Setup(r => r.IsWhitelistedInSemesterAsync(user.Email, semester.SemesterId)).ReturnsAsync(true);
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(leaderId, semester.SemesterId)).ReturnsAsync((Team?)null);
            _mockTeamRepository.Setup(r => r.GetTeamCodesBySemesterAsync(semester.SemesterId)).ReturnsAsync(new List<string>());
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>())).ReturnsAsync(new Team { TeamName = "Team A" });

            // Act
            var result = await _teamService.CreateTeamAsync(leaderId, createDto);

            // Assert
            result.Should().NotBeNull();
            result.TeamName.Should().Be("Team A");
        }

        [Fact]
        public async Task CreateTeamAsync_ShouldThrow_WhenInaccessibleStage()
        {
            // Arrange
            var semester = new Semester { Status = "In Progress" }; // Not Open
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.CreateTeamAsync(1, new CreateTeamDTO()));
        }

        [Fact]
        public async Task UpdateTeamAsync_ShouldSucceed_WhenValid()
        {
            // Arrange
            int teamId = 1, leaderId = 1;
            var updateDto = new UpdateTeamDTO { TeamName = "New Name", Description = "Desc" };
            var team = new Team { TeamId = teamId, LeaderId = leaderId, Status = "Active" };
            var semester = new Semester { Status = "Open" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team)).ReturnsAsync(true);

            // Act
            var result = await _teamService.UpdateTeamAsync(teamId, leaderId, updateDto);

            // Assert
            result.TeamName.Should().Be("New Name");
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldSucceed_WhenOpen()
        {
            // Arrange
            var semester = new Semester { Status = "Open" };
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockTeamMemberRepository.Setup(r => r.RemoveMemberAsync(1, 1)).ReturnsAsync(true);
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Team { TeamId = 1 });

            // Act
            var result = await _teamService.RemoveMemberAsync(1, 1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrow_WhenNotOpen()
        {
            // Arrange
            var semester = new Semester { Status = "In Progress" };
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.RemoveMemberAsync(1, 1));
        }

        [Fact]
        public async Task ChangeLeaderAsync_ShouldSucceed_WhenValid()
        {
            // Arrange
            int teamId = 1, oldLeaderId = 1, newLeaderId = 2;
            var team = new Team 
            { 
                TeamId = teamId, 
                LeaderId = oldLeaderId,
                Teammembers = new List<Teammember> 
                { 
                    new Teammember { StudentId = oldLeaderId, Role = "Leader" },
                    new Teammember { StudentId = newLeaderId, Role = "Member" }
                }
            };
            var semester = new Semester { Status = "Open" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(oldLeaderId)).ReturnsAsync(new List<Thesis>());

            // Act
            var result = await _teamService.ChangeLeaderAsync(teamId, oldLeaderId, newLeaderId);

            // Assert
            result.Should().BeTrue();
            team.LeaderId.Should().Be(newLeaderId);
        }
    }
}
