using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Moq;
using Repositories;
using Services;
using Services.Helpers;
using BusinessObjects.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using BusinessObjects;

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
                SemesterId = 1
            };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(team);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team))
                .ReturnsAsync(true);
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
            _mockSemesterService.Verify(s => s.InvalidateSemesterCacheAsync(1), Times.Once);
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
        public async Task DisbandTeamAsync_ThrowsUnauthorizedAccessException_WhenNotLeader()
        {
            // Arrange
            var team = new Team { TeamId = 1, LeaderId = 999 };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(team);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _teamService.DisbandTeamAsync(1, 1));
        }

        [Fact]
        public async Task DisbandTeamAsync_ThrowsInvalidOperationException_WhenThesisIsPublished()
        {
            // Arrange
            int teamId = 1;
            int leaderId = 100;
            var team = new Team { TeamId = teamId, LeaderId = leaderId };
            var thesis = new Thesis { TeamId = teamId, Status = "Published" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId))
                .ReturnsAsync(new List<Thesis> { thesis });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.DisbandTeamAsync(teamId, leaderId));
        }

        [Fact]
        public async Task UpdateTeamAsync_ShouldUpdateNameAndDescription_WhenValid()
        {
            // Arrange
            int teamId = 1;
            int leaderId = 1;
            var updateDto = new UpdateTeamDTO
            {
                TeamName = "Updated Name",
                Description = "Updated Description",
            };

            var existingTeam = new Team
            {
                TeamId = teamId,
                LeaderId = leaderId,
                TeamName = "Old Name",
                Description = "Old Description",
                Teammembers = new List<Teammember>()
            };

            _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(existingTeam);

            // Act
            var result = await _teamService.UpdateTeamAsync(teamId, leaderId, updateDto);

            // Assert
            result.TeamName.Should().Be("Updated Name");
            result.Description.Should().Be("Updated Description");
            _mockTeamRepository.Verify(x => x.UpdateAsync(existingTeam), Times.Once);
        }

        [Fact]
        public async Task UpdateTeamAsync_ShouldUploadAvatar_WhenFileProvided()
        {
            // Arrange
            int teamId = 1;
            int leaderId = 1;
            var mockFile = new Mock<IFormFile>();
            var updateDto = new UpdateTeamDTO
            {
                TeamName = "Updated Name",
                Description = "Updated Description",
                AvatarFile = mockFile.Object
            };

            var existingTeam = new Team
            {
                TeamId = teamId,
                LeaderId = leaderId,
                TeamAvatar = "old_url",
                Teammembers = new List<Teammember>()
            };

            _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(existingTeam);
            _mockCloudinaryHelper.Setup(x => x.UploadImageAsync(mockFile.Object)).ReturnsAsync("new_secure_url");

            // Act
            var result = await _teamService.UpdateTeamAsync(teamId, leaderId, updateDto);

            // Assert
            result.TeamAvatar.Should().Be("new_secure_url");
            existingTeam.TeamAvatar.Should().Be("new_secure_url");
            _mockCloudinaryHelper.Verify(x => x.UploadImageAsync(mockFile.Object), Times.Once);
        }

        [Fact]
        public async Task UpdateTeamAsync_ShouldThrow_WhenTeamNotFound()
        {
            // Arrange
            int teamId = 99;
            _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync((Team)null!);

            // Act
            Func<Task> act = async () => await _teamService.UpdateTeamAsync(teamId, 1, new UpdateTeamDTO());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Team not found");
        }

        [Fact]
        public async Task UpdateTeamAsync_ShouldThrow_WhenUserNotLeader()
        {
            // Arrange
            int teamId = 1;
            int leaderId = 1;
            int otherUserId = 2;

            var existingTeam = new Team
            {
                TeamId = teamId,
                LeaderId = leaderId,
                Teammembers = new List<Teammember>()
            };

            _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(existingTeam);

            // Act
            Func<Task> act = async () => await _teamService.UpdateTeamAsync(teamId, otherUserId, new UpdateTeamDTO());

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Only the team leader can update team information.");
        }

        [Fact]
        public async Task CreateTeamAsync_ShouldGenerateCorrectCode_WhenNoTeamsExist()
        {
            // Arrange
            int userId = 1;
            var createDto = new CreateTeamDTO { TeamName = "New Team" };
            var semester = new Semester { SemesterId = 1, SemesterCode = "FA24", Status = CampusConstants.SemesterStatus.Active };
            
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { UserId = userId, Email = "test@edu.vn" });
            _mockWhitelistRepository.Setup(r => r.IsWhitelistedInSemesterAsync("test@edu.vn", 1)).ReturnsAsync(true);
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(userId, 1)).ReturnsAsync((Team?)null);
            _mockTeamRepository.Setup(r => r.GetTeamCodesBySemesterAsync(1)).ReturnsAsync(new List<string>());
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>())).ReturnsAsync((Team t) => { t.TeamId = 1; return t; });

            // Act
            var result = await _teamService.CreateTeamAsync(userId, createDto);

            // Assert
            result.TeamCode.Should().Be("SE_01");
        }

        [Fact]
        public async Task CreateTeamAsync_ShouldIncrementCode_WhenTeamsExist()
        {
            // Arrange
            int userId = 2;
            var createDto = new CreateTeamDTO { TeamName = "Team 2" };
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP25", Status = CampusConstants.SemesterStatus.Active };
            
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { UserId = userId, Email = "test@edu.vn" });
            _mockWhitelistRepository.Setup(r => r.IsWhitelistedInSemesterAsync("test@edu.vn", 1)).ReturnsAsync(true);
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(userId, 1)).ReturnsAsync((Team?)null);
            _mockTeamRepository.Setup(r => r.GetTeamCodesBySemesterAsync(1))
                .ReturnsAsync(new List<string> { "SP25_SE_01", "SP25_SE_02", "SP25_SE_15" });
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>())).ReturnsAsync((Team t) => { t.TeamId = 1; return t; });

            // Act
            var result = await _teamService.CreateTeamAsync(userId, createDto);

            // Assert
            result.TeamCode.Should().Be("SE_16");
        }

        #region ToggleSpecialFlagAsync Tests

        [Fact]
        public async Task ToggleSpecialFlagAsync_ShouldMarkAsSpecial_WhenHodCallsOnNonSpecialTeam()
        {
            // Arrange
            int teamId = 1;
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var team = new Team { TeamId = teamId, IsSpecial = false };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team)).ReturnsAsync(true);

            // Act
            var result = await _teamService.ToggleSpecialFlagAsync(teamId, hodUserId);

            // Assert
            result.Should().BeTrue();
            team.IsSpecial.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.UpdateAsync(team), Times.Once);
            _mockSemesterService.Verify(s => s.InvalidateSemesterCacheAsync(null), Times.Once);
        }

        [Fact]
        public async Task ToggleSpecialFlagAsync_ShouldUnmarkAsSpecial_WhenHodCallsOnSpecialTeam()
        {
            // Arrange
            int teamId = 1;
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var team = new Team { TeamId = teamId, IsSpecial = true };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team)).ReturnsAsync(true);

            // Act
            var result = await _teamService.ToggleSpecialFlagAsync(teamId, hodUserId);

            // Assert
            result.Should().BeTrue();
            team.IsSpecial.Should().BeFalse();
            _mockTeamRepository.Verify(r => r.UpdateAsync(team), Times.Once);
        }

        [Fact]
        public async Task ToggleSpecialFlagAsync_ShouldReturnFalse_WhenTeamNotFound()
        {
            // Arrange
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockTeamRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Team?)null);

            // Act
            var result = await _teamService.ToggleSpecialFlagAsync(999, hodUserId);

            // Assert
            result.Should().BeFalse();
            _mockTeamRepository.Verify(r => r.UpdateAsync(It.IsAny<Team>()), Times.Never);
        }

        [Fact]
        public async Task ToggleSpecialFlagAsync_ShouldThrowUnauthorized_WhenUserIsNotHod()
        {
            // Arrange
            int userId = 5;
            var studentUser = new User { UserId = userId, Role = new Role { RoleName = "Student" } };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(studentUser);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _teamService.ToggleSpecialFlagAsync(1, userId));
        }

        [Fact]
        public async Task ToggleSpecialFlagAsync_ShouldThrowUnauthorized_WhenUserNotFound()
        {
            // Arrange
            _mockUserRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _teamService.ToggleSpecialFlagAsync(1, 999));
        }

        #endregion

        #region ForceCreateTeamAsync Tests

        [Fact]
        public async Task ForceCreateTeamAsync_ShouldCreateTeam_WhenValid()
        {
            // Arrange
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP26" };
            var student1 = new User { UserId = 100, Email = "s1@fpt.edu.vn", FullName = "Student 1", Role = new Role { RoleName = "Student" } };
            var student2 = new User { UserId = 101, Email = "s2@fpt.edu.vn", FullName = "Student 2", Role = new Role { RoleName = "Student" } };

            var dto = new ForceCreateTeamDTO
            {
                TeamName = "Force Team",
                SemesterId = 1,
                LeaderEmail = "s1@fpt.edu.vn",
                MemberEmails = new List<string> { "s1@fpt.edu.vn", "s2@fpt.edu.vn" }
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(1)).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetUsersByEmailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<User> { student1, student2 });
            _mockWhitelistRepository.Setup(r => r.GetBySemesterIdAsync(1))
                .ReturnsAsync(new List<Whitelist> {
                    new Whitelist { Email = "s1@fpt.edu.vn", Role = new Role { RoleName = "Student" } },
                    new Whitelist { Email = "s2@fpt.edu.vn", Role = new Role { RoleName = "Student" } }
                });
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(It.IsAny<int>(), 1)).ReturnsAsync((Team?)null);
            _mockTeamRepository.Setup(r => r.GetTeamCodesBySemesterAsync(1)).ReturnsAsync(new List<string>());
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>())).ReturnsAsync((Team t) => { t.TeamId = 1; return t; });

            // Act
            var result = await _teamService.ForceCreateTeamAsync(hodUserId, dto);

            // Assert
            result.Should().NotBeNull();
            result.TeamName.Should().Be("Force Team");
            result.Status.Should().Be(CampusConstants.TeamStatus.Active); // 2 members < 4 = Special = Qualified
            result.MemberCount.Should().Be(2);
            _mockTeamRepository.Verify(r => r.CreateAsync(It.Is<Team>(t => t.LeaderId == 100)), Times.Once);
        }

        [Fact]
        public async Task ForceCreateTeamAsync_ShouldThrow_WhenSemesterNotFound()
        {
            // Arrange
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var dto = new ForceCreateTeamDTO { TeamName = "X", SemesterId = 99, LeaderEmail = "a@x.com", MemberEmails = new List<string> { "a@x.com" } };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(99)).ReturnsAsync((Semester?)null);

            // Act & Assert
            Func<Task> act = () => _teamService.ForceCreateTeamAsync(hodUserId, dto);
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*99*");
        }

        [Fact]
        public async Task ForceCreateTeamAsync_ShouldThrow_WhenLeaderNotInMembers()
        {
            // Arrange
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP26" };
            var dto = new ForceCreateTeamDTO
            {
                TeamName = "X",
                SemesterId = 1,
                LeaderEmail = "leader@fpt.edu.vn",
                MemberEmails = new List<string> { "other@fpt.edu.vn" } // Leader not in list
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(1)).ReturnsAsync(semester);

            // Act & Assert
            Func<Task> act = () => _teamService.ForceCreateTeamAsync(hodUserId, dto);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Leader*members*");
        }

        [Fact]
        public async Task ForceCreateTeamAsync_ShouldThrow_WhenStudentAlreadyInTeam()
        {
            // Arrange
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP26" };
            var student = new User { UserId = 100, Email = "s1@fpt.edu.vn", FullName = "S1", Role = new Role { RoleName = "Student" } };
            var existingTeam = new Team { TeamId = 5, TeamName = "Existing Team" };

            var dto = new ForceCreateTeamDTO
            {
                TeamName = "New Team",
                SemesterId = 1,
                LeaderEmail = "s1@fpt.edu.vn",
                MemberEmails = new List<string> { "s1@fpt.edu.vn" }
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(1)).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetUsersByEmailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<User> { student });
            _mockWhitelistRepository.Setup(r => r.GetBySemesterIdAsync(1))
                .ReturnsAsync(new List<Whitelist> {
                    new Whitelist { Email = "s1@fpt.edu.vn", Role = new Role { RoleName = "Student" } }
                });
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(100, 1)).ReturnsAsync(existingTeam);

            // Act & Assert
            Func<Task> act = () => _teamService.ForceCreateTeamAsync(hodUserId, dto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already in team*");
        }

        #endregion

        #region CreateTeamAsync Additional Edge Cases

        [Fact]
        public async Task CreateTeamAsync_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            // We mock the semester to exist so the flow reaches the user check.
            int userId = 999;
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP25" };

            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            // Return null to simulate that the user doesn't exist in the system.
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _teamService.CreateTeamAsync(userId, new CreateTeamDTO { TeamName = "Ghost Team" });

            // Assert
            // The service should detect a missing user and throw a descriptive exception.
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task CreateTeamAsync_ShouldThrow_WhenUserAlreadyInTeam()
        {
            // Arrange
            int userId = 5;
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP25" };
            var existingTeam = new Team { TeamId = 10, TeamName = "Old Team" };

            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(new User { UserId = userId, Email = "student@edu.vn" });
            _mockWhitelistRepository.Setup(r => r.IsWhitelistedInSemesterAsync("student@edu.vn", 1))
                .ReturnsAsync(true);
            // The student is already in a team for this semester.
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(userId, 1))
                .ReturnsAsync(existingTeam);

            // Act
            Func<Task> act = () => _teamService.CreateTeamAsync(userId, new CreateTeamDTO { TeamName = "New Team" });

            // Assert
            // System should detect the conflict with existing team membership and raise error.
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateTeamAsync_ShouldThrow_WhenUserNotWhitelisted()
        {
            // Arrange
            int userId = 6;
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP25" };

            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(new User { UserId = userId, Email = "notwhitelisted@edu.vn" });
            // Whitelist check returns false, meaning the student is not eligible.
            _mockWhitelistRepository.Setup(r => r.IsWhitelistedInSemesterAsync("notwhitelisted@edu.vn", 1))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = () => _teamService.CreateTeamAsync(userId, new CreateTeamDTO { TeamName = "Rejected Team" });

            // Assert
            // Not whitelisted students should not be able to create teams.
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task CreateTeamAsync_ShouldThrow_WhenNoActiveSemester()
        {
            // Arrange
            // The semester repository returns null, simulating no active semester.
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync((Semester?)null);

            // Act
            Func<Task> act = () => _teamService.CreateTeamAsync(1, new CreateTeamDTO { TeamName = "Semester Missing" });

            // Assert
            // Without a semester context, team creation must be blocked.
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        #endregion

        #region DisbandTeamAsync Additional Edge Cases

        [Fact]
        public async Task DisbandTeamAsync_ShouldSucceed_WhenThesisIsDraft()
        {
            // Arrange
            int teamId = 2;
            int leaderId = 200;
            var team = new Team { TeamId = teamId, LeaderId = leaderId, Status = "Insufficient", SemesterId = 2 };

            // The team has a thesis but it's only in Draft status (not blocking disbanding).
            var draftThesis = new Thesis { TeamId = teamId, Status = "Draft" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team)).ReturnsAsync(true);
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId))
                .ReturnsAsync(new List<Thesis> { draftThesis });
            _mockTeamMemberRepository.Setup(r => r.RemoveAllMembersFromTeamAsync(teamId)).ReturnsAsync(true);

            // Act
            var result = await _teamService.DisbandTeamAsync(teamId, leaderId);

            // Assert
            // Draft thesis should not block disbanding.
            result.Should().BeTrue();
            team.Status.Should().Be("Disbanded");
        }

        [Fact]
        public async Task DisbandTeamAsync_ShouldCancelThesis_WhenThesisIsReviewing()
        {
            // Arrange
            int teamId = 3;
            int leaderId = 300;
            var team = new Team { TeamId = teamId, LeaderId = leaderId, Status = "Active" };
            // Thesis under review disbanding should cancel it.
            var reviewThesis = new Thesis { TeamId = teamId, Status = "Reviewing" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team)).ReturnsAsync(true);
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId))
                .ReturnsAsync(new List<Thesis> { reviewThesis });
            _mockTeamMemberRepository.Setup(r => r.RemoveAllMembersFromTeamAsync(teamId)).ReturnsAsync(true);

            // Act
            var result = await _teamService.DisbandTeamAsync(teamId, leaderId);

            // Assert
            result.Should().BeTrue();
            reviewThesis.Status.Should().Be("Cancelled");
            _mockThesisRepository.Verify(r => r.UpdateThesisAsync(reviewThesis), Times.Once);
            team.Status.Should().Be("Disbanded");
            _mockTeamRepository.Verify(r => r.UpdateAsync(team), Times.Once);
        }

        [Fact]
        public async Task DisbandTeamAsync_ShouldInvalidateCache_AfterDisband()
        {
            // Arrange
            int teamId = 4;
            int leaderId = 400;
            int semesterId = 5;
            var team = new Team { TeamId = teamId, LeaderId = leaderId, Status = "Insufficient", SemesterId = semesterId };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team)).ReturnsAsync(true);
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId))
                .ReturnsAsync(new List<Thesis>());
            _mockTeamMemberRepository.Setup(r => r.RemoveAllMembersFromTeamAsync(teamId)).ReturnsAsync(true);

            // Act
            await _teamService.DisbandTeamAsync(teamId, leaderId);

            // Assert
            // Cache invalidation ensures other users see up-to-date team lists.
            _mockSemesterService.Verify(s => s.InvalidateSemesterCacheAsync(semesterId), Times.Once);
        }

        #endregion

        #region UpdateTeamAsync Additional Edge Cases

        [Fact]
        public async Task UpdateTeamAsync_ShouldPreserveAvatar_WhenNoFileProvided()
        {
            // Arrange
            int teamId = 1;
            int leaderId = 1;
            string originalAvatar = "https://res.cloudinary.com/old_avatar.jpg";

            var updateDto = new UpdateTeamDTO
            {
                TeamName = "New Name Only",
                Description = "Same description",
                AvatarFile = null // No new file â€” avatar should remain unchanged.
            };

            var existingTeam = new Team
            {
                TeamId = teamId,
                LeaderId = leaderId,
                TeamName = "Old Name",
                TeamAvatar = originalAvatar,
                Teammembers = new List<Teammember>()
            };

            _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(existingTeam);

            // Act
            var result = await _teamService.UpdateTeamAsync(teamId, leaderId, updateDto);

            // Assert
            // Avatar should not be changed since no file was provided.
            result.TeamAvatar.Should().Be(originalAvatar);
            // Cloud upload should NOT have been called.
            _mockCloudinaryHelper.Verify(x => x.UploadImageAsync(It.IsAny<IFormFile>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTeamAsync_ShouldUpdateDescription_WhenNameNotChanged()
        {
            // Arrange
            int teamId = 1;
            int leaderId = 1;
            string originalName = "Unchanged Team Name";

            var updateDto = new UpdateTeamDTO
            {
                TeamName = originalName, // Same name.
                Description = "Brand new detailed description of the project scope."
            };

            var existingTeam = new Team
            {
                TeamId = teamId,
                LeaderId = leaderId,
                TeamName = originalName,
                Description = "Old description.",
                Teammembers = new List<Teammember>()
            };

            _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(existingTeam);

            // Act
            var result = await _teamService.UpdateTeamAsync(teamId, leaderId, updateDto);

            // Assert
            result.TeamName.Should().Be(originalName);
            result.Description.Should().Be("Brand new detailed description of the project scope.");
            // Repository update should still be called.
            _mockTeamRepository.Verify(x => x.UpdateAsync(existingTeam), Times.Once);
        }

        #endregion

        #region ToggleSpecialFlagAsync Additional Edge Cases

        [Fact]
        public async Task ToggleSpecialFlagAsync_ShouldInvalidateCache_WhenToggledToSpecial()
        {
            // Arrange
            int teamId = 10;
            int hodUserId = 5;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var team = new Team { TeamId = teamId, IsSpecial = false };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockTeamRepository.Setup(r => r.UpdateAsync(team)).ReturnsAsync(true);

            // Act
            await _teamService.ToggleSpecialFlagAsync(teamId, hodUserId);

            // Assert
            // Global cache invalidation ensures all semesters reflect the change.
            _mockSemesterService.Verify(s => s.InvalidateSemesterCacheAsync(null), Times.Once);
        }

        [Fact]
        public async Task ToggleSpecialFlagAsync_ShouldThrow_WhenUserIsStudent()
        {
            // Arrange
            int userId = 7;
            // Student role â€” should be blocked from this administrative action.
            var student = new User { UserId = userId, Role = new Role { RoleName = "Student" } };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(student);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _teamService.ToggleSpecialFlagAsync(1, userId));

            // Make sure we never even looked up the team.
            _mockTeamRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ToggleSpecialFlagAsync_ShouldThrow_WhenUserIsLecturer()
        {
            // Arrange
            int userId = 8;
            var lecturer = new User { UserId = userId, Role = new Role { RoleName = "Lecturer" } };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(lecturer);

            // Act & Assert
            // Only HOD should have the privilege to toggle this flag.
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _teamService.ToggleSpecialFlagAsync(1, userId));
        }

        #endregion

        #region ForceCreateTeamAsync Additional Edge Cases

        [Fact]
        public async Task ForceCreateTeamAsync_ShouldThrow_WhenUserIsNotHod()
        {
            // Arrange
            int studentUserId = 5;
            var student = new User { UserId = studentUserId, Role = new Role { RoleName = "Student" } };
            var dto = new ForceCreateTeamDTO
            {
                TeamName = "Unauthorized Attempt",
                SemesterId = 1,
                LeaderEmail = "s1@fpt.edu.vn",
                MemberEmails = new List<string> { "s1@fpt.edu.vn" }
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(studentUserId)).ReturnsAsync(student);

            // Act & Assert
            // Students should not be able to force-create teams under any circumstances.
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _teamService.ForceCreateTeamAsync(studentUserId, dto));
        }

        [Fact]
        public async Task ForceCreateTeamAsync_ShouldThrow_WhenMemberEmailsIsEmpty()
        {
            // Arrange
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP26" };
            var dto = new ForceCreateTeamDTO
            {
                TeamName = "Empty Members",
                SemesterId = 1,
                LeaderEmail = "leader@fpt.edu.vn",
                // MemberEmails is empty â€” this is an invalid request.
                MemberEmails = new List<string>()
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(1)).ReturnsAsync(semester);

            // Act & Assert
            // A team with no members is semantically invalid.
            await Assert.ThrowsAsync<ArgumentException>(
                () => _teamService.ForceCreateTeamAsync(hodUserId, dto));
        }

        [Fact]
        public async Task ForceCreateTeamAsync_ShouldCreateWithQualifiedStatus_WhenFourMembersAdded()
        {
            // Arrange
            int hodUserId = 10;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP26" };

            // Create 4 students â€” this should produce a "Qualified" or "Active" team.
            var students = Enumerable.Range(1, 4).Select(i => new User
            {
                UserId = 100 + i,
                Email = $"s{i}@fpt.edu.vn",
                FullName = $"Student {i}",
                Role = new Role { RoleName = "Student" }
            }).ToList();

            var emails = students.Select(s => s.Email).ToList();

            var dto = new ForceCreateTeamDTO
            {
                TeamName = "Full Team",
                SemesterId = 1,
                LeaderEmail = "s1@fpt.edu.vn",
                MemberEmails = emails
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockSemesterRepository.Setup(r => r.GetSemesterByIdAsync(1)).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetUsersByEmailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(students);
            _mockWhitelistRepository.Setup(r => r.GetBySemesterIdAsync(1))
                .ReturnsAsync(emails.Select(e => new Whitelist
                {
                    Email = e,
                    Role = new Role { RoleName = "Student" }
                }).ToList());
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(It.IsAny<int>(), 1))
                .ReturnsAsync((Team?)null);
            _mockTeamRepository.Setup(r => r.GetTeamCodesBySemesterAsync(1))
                .ReturnsAsync(new List<string>());
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>()))
                .ReturnsAsync((Team t) => { t.TeamId = 99; return t; });

            // Act
            var result = await _teamService.ForceCreateTeamAsync(hodUserId, dto);

            // Assert
            result.Should().NotBeNull();
            result.MemberCount.Should().Be(4);
            // 4 members meets minimum so team should NOT be "Insufficient".
            result.Status.Should().NotBe("Insufficient");
        }

        #endregion
    }
}

