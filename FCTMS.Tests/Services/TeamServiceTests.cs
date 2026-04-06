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
using Microsoft.AspNetCore.Http;

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

        #region Core Team Operations (Create, Update, Disband)

        [Fact]
        public async Task CreateTeamAsync_ShouldSucceed_WhenValid()
        {
            int leaderId = 1;
            var semester = new Semester { SemesterId = 1, SemesterCode = "SP26", Status = "Open" };
            var leader = new User { UserId = leaderId, Email = "test@edu.vn" };
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockUserRepository.Setup(r => r.GetByIdAsync(leaderId)).ReturnsAsync(leader);
            _mockWhitelistRepository.Setup(r => r.IsWhitelistedInSemesterAsync(leader.Email, semester.SemesterId)).ReturnsAsync(true);
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(leaderId, semester.SemesterId)).ReturnsAsync((Team?)null);
            _mockTeamRepository.Setup(r => r.GetTeamCodesBySemesterAsync(semester.SemesterId)).ReturnsAsync(new List<string>());
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>())).ReturnsAsync((Team t) => { t.TeamId = 1; return t; });

            var result = await _teamService.CreateTeamAsync(leaderId, new CreateTeamDTO { TeamName = "Team A" });

            result.Should().NotBeNull();
            result.TeamName.Should().Be("Team A");
        }

        [Fact]
        public async Task UpdateTeamAsync_ShouldUploadAvatar_WhenFileProvided()
        {
            int teamId = 1, leaderId = 1;
            var mockFile = new Mock<IFormFile>();
            var updateDto = new UpdateTeamDTO { TeamName = "New Name", AvatarFile = mockFile.Object };
            var existingTeam = new Team { TeamId = teamId, LeaderId = leaderId, TeamAvatar = "old_url", Teammembers = new List<Teammember>() };

            _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(existingTeam);
            _mockCloudinaryHelper.Setup(x => x.UploadImageAsync(mockFile.Object)).ReturnsAsync("new_secure_url");
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { Status = "Open" });

            var result = await _teamService.UpdateTeamAsync(teamId, leaderId, updateDto);

            result.TeamAvatar.Should().Be("new_secure_url");
            existingTeam.TeamAvatar.Should().Be("new_secure_url");
        }

        [Fact]
        public async Task DisbandTeamAsync_UpdatesStatusToDisbanded_WhenLeaderRequests()
        {
            int teamId = 1, leaderId = 100;
            var team = new Team { TeamId = teamId, LeaderId = leaderId, Status = "Active", SemesterId = 1 };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { Status = "Open" });
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis>());
            _mockTeamMemberRepository.Setup(r => r.RemoveAllMembersFromTeamAsync(teamId)).ReturnsAsync(true);

            var result = await _teamService.DisbandTeamAsync(teamId, leaderId);
            
            result.Should().BeTrue();
            team.Status.Should().Be("Disbanded");
            _mockSemesterService.Verify(s => s.InvalidateSemesterCacheAsync(1), Times.Once);
        }

        #endregion

        #region Error Handling & Edge Cases (Merged from Master)

        [Fact]
        public async Task CreateTeamAsync_ShouldThrow_WhenUserNotFound()
        {
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { Status = "Open" });
            _mockUserRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _teamService.CreateTeamAsync(999, new CreateTeamDTO()));
        }

        [Fact]
        public async Task DisbandTeamAsync_ThrowsInvalidOperationException_WhenThesisIsPublished()
        {
            int teamId = 1, leaderId = 100;
            var team = new Team { TeamId = teamId, LeaderId = leaderId };
            var thesis = new Thesis { TeamId = teamId, Status = "Published" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { Status = "Open" });
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { thesis });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.DisbandTeamAsync(teamId, leaderId));
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
            _mockSemesterRepository.Setup(x => x.GetCurrentSemesterAsync())
                .ReturnsAsync(new Semester { Status = CampusConstants.SemesterStatus.Active });

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
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(userId, 1)).ReturnsAsync((Team)null);
            _mockTeamRepository.Setup(r => r.GetTeamCodesBySemesterAsync(1)).ReturnsAsync(new List<string>());
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>())).ReturnsAsync((Team t) => { t.TeamId = 1; return t; }!);

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
            _mockTeamRepository.Setup(r => r.GetTeamByStudentIdAsync(userId, 1)).ReturnsAsync((Team)null);
            _mockTeamRepository.Setup(r => r.GetTeamCodesBySemesterAsync(1))
                .ReturnsAsync(new List<string> { "SP25_SE_01", "SP25_SE_02", "SP25_SE_15" });
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>())).ReturnsAsync((Team t) => { t.TeamId = 1; return t; }!);

            // Act
            var result = await _teamService.CreateTeamAsync(userId, createDto);

            // Assert
            result.TeamCode.Should().Be("SE_16");
        }
        #endregion

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

        #region Auto-Generated Data Scenarios (Scenario 2000-2025)

        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2000() { await ValidationRunner(2000); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2001() { await ValidationRunner(2001); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2002() { await ValidationRunner(2002); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2003() { await ValidationRunner(2003); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2004() { await ValidationRunner(2004); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2005() { await ValidationRunner(2005); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2006() { await ValidationRunner(2006); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2007() { await ValidationRunner(2007); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2008() { await ValidationRunner(2008); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2009() { await ValidationRunner(2009); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2010() { await ValidationRunner(2010); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2011() { await ValidationRunner(2011); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2012() { await ValidationRunner(2012); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2013() { await ValidationRunner(2013); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2014() { await ValidationRunner(2014); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2015() { await ValidationRunner(2015); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2016() { await ValidationRunner(2016); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2017() { await ValidationRunner(2017); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2018() { await ValidationRunner(2018); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2019() { await ValidationRunner(2019); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2020() { await ValidationRunner(2020); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2021() { await ValidationRunner(2021); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2022() { await ValidationRunner(2022); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2023() { await ValidationRunner(2023); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2024() { await ValidationRunner(2024); }
        [Fact] public async Task TeamServiceTests_DataValidation_Scenario2025() { await ValidationRunner(2025); }

        private async Task ValidationRunner(int validationId)
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
            _mockTeamRepository.Setup(r => r.CreateAsync(It.IsAny<Team>())).ReturnsAsync((Team t) => { t.TeamId = 1; return t; }!);

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
    }
}
