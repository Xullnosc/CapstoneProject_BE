using BusinessObjects.DTOs;
using BusinessObjects.Models;
using BusinessObjects;
using Moq;
using Repositories;
using Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration; // Added
using Microsoft.Extensions.Logging;
using System.Linq; // Added

namespace FCTMS.Tests.Services
{
    public class TeamInvitationServiceTests
    {
        private readonly Mock<ITeamInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ITeamMemberRepository> _mockTeamMemberRepository;
        private readonly Mock<ITeamRepository> _mockTeamRepository;
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IWhitelistRepository> _mockWhitelistRepository;
        private readonly Mock<IEmailService> _mockEmailService; // Added
        private readonly Mock<IConfiguration> _mockConfiguration; // Added
        private readonly Mock<ISemesterService> _mockSemesterService;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILogger<TeamInvitationService>> _mockLogger;
        private readonly TeamInvitationService _service;

        public TeamInvitationServiceTests()
        {
            _mockInvitationRepository = new Mock<ITeamInvitationRepository>();
            _mockTeamMemberRepository = new Mock<ITeamMemberRepository>();
            _mockTeamRepository = new Mock<ITeamRepository>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockWhitelistRepository = new Mock<IWhitelistRepository>();
            _mockEmailService = new Mock<IEmailService>(); // Added
            _mockConfiguration = new Mock<IConfiguration>(); // Added
            _mockSemesterService = new Mock<ISemesterService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<TeamInvitationService>>();

            // Setup Email Configuration Mocks
            // Email template is now hardcoded, no need to mock configuration for it.
            _mockConfiguration.Setup(c => c["AllowedOrigins"]).Returns("http://localhost:5173");

            _service = new TeamInvitationService(
                _mockInvitationRepository.Object,
                _mockTeamMemberRepository.Object,
                _mockTeamRepository.Object,
                _mockSemesterRepository.Object,
                _mockUserRepository.Object,
                _mockWhitelistRepository.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object,
                _mockSemesterService.Object,
                _mockNotificationService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetMyInvitationsAsync_ShouldCallGetPendingInvitations_AndReturnMappedDTOs()
        {
            // Arrange
            int studentId = 1;
            var mockInvitations = new List<Teaminvitation>
            {
                new Teaminvitation
                {
                    InvitationId = 1,
                    TeamId = 10,
                    StudentId = studentId,
                    InvitedBy = 2,
                    Status = CampusConstants.InvitationStatus.Pending,
                    Team = new Team
                    {
                        TeamId = 10,
                        TeamName = "Test Team",
                        Teammembers = new List<Teammember>(),
                        Leader = new User { FullName = "Leader Name" }
                    },
                    InvitedByNavigation = new User { FullName = "Inviter Name" }
                }
            };

            _mockInvitationRepository.Setup(r => r.GetPendingInvitationsByStudentAsync(studentId))
                .ReturnsAsync(mockInvitations);

            // Act
            var result = await _service.GetMyInvitationsAsync(studentId);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].InvitationId);
            Assert.Equal("Test Team", result[0].Team!.TeamName);
            _mockInvitationRepository.Verify(r => r.GetPendingInvitationsByStudentAsync(studentId), Times.Once);
        }

        [Fact]
        public async Task SendInvitationAsync_ValidRequest_SendsEmailAndReturnsDTO()
        {
            // Arrange
            int teamId = 1;
            int inviterId = 2;
            string studentEmail = "student@example.com";

            var team = new Team { TeamId = teamId, LeaderId = inviterId, TeamName = "My Team", Teammembers = new List<Teammember>() };
            var inviter = new User { UserId = inviterId, FullName = "Inviter" };
            var student = new User { UserId = 3, Email = studentEmail, FullName = "Student", StudentCode = "S123" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockUserRepository.Setup(r => r.SearchUsersAsync(studentEmail)).ReturnsAsync(new List<User> { student });
            _mockUserRepository.Setup(r => r.GetByIdAsync(inviterId)).ReturnsAsync(inviter);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockTeamMemberRepository.Setup(r => r.IsStudentInTeamAsync(student.UserId, 1)).ReturnsAsync(false);
            _mockInvitationRepository.Setup(r => r.GetByTeamAndStudentAsync(teamId, student.UserId)).ReturnsAsync((Teaminvitation?)null);

            var createdInvitation = new Teaminvitation { InvitationId = 100, TeamId = teamId, StudentId = student.UserId, Status = CampusConstants.InvitationStatus.Pending };
            _mockInvitationRepository.Setup(r => r.CreateAsync(It.IsAny<Teaminvitation>())).ReturnsAsync(createdInvitation);
             _mockInvitationRepository.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(createdInvitation); // For reloading DTO

            // Act
            var result = await _service.SendInvitationAsync(teamId, inviterId, studentEmail);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.InvitationId);
            _mockEmailService.Verify(e => e.SendEmailAsync(studentEmail, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockInvitationRepository.Verify(r => r.CreateAsync(It.IsAny<Teaminvitation>()), Times.Once);
        }

        [Fact]
        public async Task SendInvitationAsync_TeamNotFound_ThrowsKeyNotFoundException()
        {
            _mockTeamRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Team?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SendInvitationAsync(1, 1, "test@email.com"));
        }

        [Fact]
        public async Task SendInvitationAsync_NotLeader_ThrowsUnauthorizedAccessException()
        {
            var team = new Team { TeamId = 1, LeaderId = 99 }; // Different leader
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.SendInvitationAsync(1, 1, "test@email.com"));
        }

        [Fact]
        public async Task SendInvitationAsync_StudentNotFound_ThrowsKeyNotFoundException()
        {
            var team = new Team { TeamId = 1, LeaderId = 1, Teammembers = new List<Teammember>() };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _mockUserRepository.Setup(r => r.SearchUsersAsync(It.IsAny<string>())).ReturnsAsync(new List<User>()); // No users found

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SendInvitationAsync(1, 1, "unknown@email.com"));
        }

        [Fact]
        public async Task CancelInvitationAsync_ValidRequest_UpdatesStatusToCancelled()
        {
            // Arrange
            int invitationId = 1;
            int userId = 10; // Inviter (Leader)
            var invitation = new Teaminvitation { InvitationId = invitationId, InvitedBy = userId, Status = CampusConstants.InvitationStatus.Pending };

            _mockInvitationRepository.Setup(r => r.GetByIdAsync(invitationId)).ReturnsAsync(invitation);

            // Act
            await _service.CancelInvitationAsync(invitationId, userId);

            // Assert
            _mockInvitationRepository.Verify(r => r.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Cancelled), Times.Once);
        }

        [Fact]
        public async Task CancelInvitationAsync_InvitationNotFound_ThrowsKeyNotFoundException()
        {
            _mockInvitationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Teaminvitation?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CancelInvitationAsync(1, 1));
        }

         [Fact]
        public async Task CancelInvitationAsync_NotInviter_ThrowsUnauthorizedAccessException()
        {
             var invitation = new Teaminvitation { InvitationId = 1, InvitedBy = 55 };
             _mockInvitationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invitation);

             await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CancelInvitationAsync(1, 1));
        }

        [Fact]
        public async Task AcceptInvitationAsync_ValidRequest_AddsMemberUpdatesStatusAndCancelsOthers()
        {
            // Arrange
            int invitationId = 1;
            int studentId = 3;
            int teamId = 10;
            var invitation = new Teaminvitation { InvitationId = invitationId, StudentId = studentId, TeamId = teamId, Status = CampusConstants.InvitationStatus.Pending };
            var team = new Team { TeamId = teamId, Teammembers = new List<Teammember> { new Teammember { StudentId = 1 } }, Status = CampusConstants.TeamStatus.Insufficient };
            var currentSemester = new Semester { SemesterId = 1 };

            _mockInvitationRepository.Setup(r => r.GetByIdAsync(invitationId)).ReturnsAsync(invitation);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(currentSemester);
            _mockTeamMemberRepository.Setup(r => r.IsStudentInTeamAsync(studentId, 1)).ReturnsAsync(false);
            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);

            // Act
            await _service.AcceptInvitationAsync(invitationId, studentId);

            // Assert
            _mockTeamMemberRepository.Verify(r => r.AddMemberAsync(It.Is<Teammember>(m => m.StudentId == studentId && m.TeamId == teamId)), Times.Once);
            _mockInvitationRepository.Verify(r => r.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Accepted), Times.Once);
            _mockInvitationRepository.Verify(r => r.CancelAllPendingInvitationsForStudentAsync(studentId), Times.Once);
            // newCount = 1 (existing) + 1 (new) = 2. Insufficient status remains Insufficient if < 3. 
            // Wait, helper: if (newCount >= 3 && team.Status == Insufficient) -> Pending.
            // In this test, newCount is 2. So no status update.
            _mockTeamRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AcceptInvitationAsync_InvitationNotFound_ThrowsKeyNotFoundException()
        {
            _mockInvitationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Teaminvitation?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AcceptInvitationAsync(1, 1));
        }

        [Fact]
        public async Task AcceptInvitationAsync_NotRecipient_ThrowsUnauthorizedAccessException()
        {
            var invitation = new Teaminvitation { InvitationId = 1, StudentId = 99 };
            _mockInvitationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invitation);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.AcceptInvitationAsync(1, 1));
        }

        [Fact]
        public async Task AcceptInvitationAsync_NotPending_ThrowsInvalidOperationException()
        {
            var invitation = new Teaminvitation { InvitationId = 1, StudentId = 1, Status = CampusConstants.InvitationStatus.Accepted };
            _mockInvitationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invitation);
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptInvitationAsync(1, 1));
        }

        [Fact]
        public async Task AcceptInvitationAsync_StudentAlreadyInTeam_ThrowsInvalidOperationException()
        {
            var invitation = new Teaminvitation { InvitationId = 1, StudentId = 1, Status = CampusConstants.InvitationStatus.Pending };
            _mockInvitationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invitation);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockTeamMemberRepository.Setup(r => r.IsStudentInTeamAsync(1, 1)).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptInvitationAsync(1, 1));
        }

        [Fact]
        public async Task AcceptInvitationAsync_TeamNotFound_ThrowsKeyNotFoundException()
        {
            var invitation = new Teaminvitation { InvitationId = 1, StudentId = 1, Status = CampusConstants.InvitationStatus.Pending, TeamId = 10 };
            _mockInvitationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invitation);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockTeamMemberRepository.Setup(r => r.IsStudentInTeamAsync(1, 1)).ReturnsAsync(false);
            _mockTeamRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Team?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AcceptInvitationAsync(1, 1));
        }

        [Fact]
        public async Task AcceptInvitationAsync_TeamFull_ThrowsInvalidOperationException()
        {
            var invitation = new Teaminvitation { InvitationId = 1, StudentId = 1, Status = CampusConstants.InvitationStatus.Pending, TeamId = 10 };
            var team = new Team { TeamId = 10, Teammembers = new List<Teammember> { new(), new(), new(), new(), new() } }; // 5 members

            _mockInvitationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invitation);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockTeamMemberRepository.Setup(r => r.IsStudentInTeamAsync(1, 1)).ReturnsAsync(false);
            _mockTeamRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(team);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptInvitationAsync(1, 1));
        }

        [Fact]
        public async Task DeclineInvitationAsync_ValidRequest_UpdatesStatusToDeclined()
        {
            // Arrange
            int invitationId = 1;
            int studentId = 1;
            var invitation = new Teaminvitation { InvitationId = invitationId, StudentId = studentId, Status = CampusConstants.InvitationStatus.Pending };

            _mockInvitationRepository.Setup(r => r.GetByIdAsync(invitationId)).ReturnsAsync(invitation);

            // Act
            await _service.DeclineInvitationAsync(invitationId, studentId);

            // Assert
            _mockInvitationRepository.Verify(r => r.UpdateStatusAsync(invitationId, CampusConstants.InvitationStatus.Declined), Times.Once);
        }

        [Fact]
        public async Task DeclineInvitationAsync_InvitationNotFound_ThrowsKeyNotFoundException()
        {
            _mockInvitationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Teaminvitation?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeclineInvitationAsync(1, 1));
        }

        [Fact]
        public async Task SendInvitationAsync_StudentInWhitelistButNotInUsers_CreatesUserAndSendsEmail()
        {
            // Arrange
            int teamId = 1;
            int inviterId = 2;
            string studentEmail = "new_student@example.com";
            var team = new Team { TeamId = teamId, LeaderId = inviterId, TeamName = "My Team", Teammembers = new List<Teammember>() };
            var whitelistEntry = new Whitelist { Email = studentEmail, FullName = "New Student", RoleId = 1, StudentCode = "S456" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockUserRepository.Setup(r => r.SearchUsersAsync(studentEmail)).ReturnsAsync(new List<User>()); // Not in users
            _mockWhitelistRepository.Setup(r => r.GetByEmailAsync(studentEmail)).ReturnsAsync(whitelistEntry);
            _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(new User { UserId = 5, Email = studentEmail });
            _mockUserRepository.Setup(r => r.GetByIdAsync(inviterId)).ReturnsAsync(new User { FullName = "Inviter" });
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockTeamMemberRepository.Setup(r => r.IsStudentInTeamAsync(5, 1)).ReturnsAsync(false);
            _mockInvitationRepository.Setup(r => r.GetByTeamAndStudentAsync(teamId, 5)).ReturnsAsync((Teaminvitation?)null);
            _mockInvitationRepository.Setup(r => r.CreateAsync(It.IsAny<Teaminvitation>())).ReturnsAsync(new Teaminvitation { InvitationId = 101 });
            _mockInvitationRepository.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(new Teaminvitation { InvitationId = 101 });

            // Act
            var result = await _service.SendInvitationAsync(teamId, inviterId, studentEmail);

            // Assert
            Assert.NotNull(result);
            _mockUserRepository.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == studentEmail)), Times.Once);
            _mockEmailService.Verify(e => e.SendEmailAsync(studentEmail, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendInvitationAsync_AlreadyInvited_ThrowsInvalidOperationException()
        {
            // Arrange
            int teamId = 1;
            int studentId = 3;
            var team = new Team { TeamId = teamId, LeaderId = 1, Teammembers = new List<Teammember>() };
            var student = new User { UserId = studentId, Email = "test@test.com" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockUserRepository.Setup(r => r.SearchUsersAsync(It.IsAny<string>())).ReturnsAsync(new List<User> { student });
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockTeamMemberRepository.Setup(r => r.IsStudentInTeamAsync(studentId, 1)).ReturnsAsync(false);
            _mockInvitationRepository.Setup(r => r.GetByTeamAndStudentAsync(teamId, studentId)).ReturnsAsync(new Teaminvitation());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendInvitationAsync(teamId, 1, "test@test.com"));
        }

        [Fact]
        public async Task SendInvitationAsync_StudentAlreadyInTeam_ThrowsInvalidOperationException()
        {
            // Arrange
            int teamId = 1;
            int studentId = 3;
            var team = new Team { TeamId = teamId, LeaderId = 1, Teammembers = new List<Teammember>() };
            var student = new User { UserId = studentId, Email = "test@test.com" };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockUserRepository.Setup(r => r.SearchUsersAsync(It.IsAny<string>())).ReturnsAsync(new List<User> { student });
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync()).ReturnsAsync(new Semester { SemesterId = 1 });
            _mockTeamMemberRepository.Setup(r => r.IsStudentInTeamAsync(studentId, 1)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendInvitationAsync(teamId, 1, "test@test.com"));
        }

        [Fact]
        public async Task SendInvitationAsync_TeamIsFull_ThrowsInvalidOperationException()
        {
            // Arrange
            var team = new Team { TeamId = 1, LeaderId = 1, Teammembers = new List<Teammember> { new(), new(), new(), new(), new() } };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendInvitationAsync(1, 1, "test@test.com"));
        }

        [Fact]
        public async Task CancelInvitationAsync_NotPending_ThrowsInvalidOperationException()
        {
            // Arrange
            int invitationId = 1;
            int userId = 10;
            var invitation = new Teaminvitation { InvitationId = invitationId, InvitedBy = userId, Status = CampusConstants.InvitationStatus.Accepted };

            _mockInvitationRepository.Setup(r => r.GetByIdAsync(invitationId)).ReturnsAsync(invitation);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelInvitationAsync(invitationId, userId));
        }
    }
}
