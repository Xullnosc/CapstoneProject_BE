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
        private readonly Mock<IEmailService> _mockEmailService; // Added
        private readonly Mock<IConfiguration> _mockConfiguration; // Added
        private readonly TeamInvitationService _service;

        public TeamInvitationServiceTests()
        {
            _mockInvitationRepository = new Mock<ITeamInvitationRepository>();
            _mockTeamMemberRepository = new Mock<ITeamMemberRepository>();
            _mockTeamRepository = new Mock<ITeamRepository>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEmailService = new Mock<IEmailService>(); // Added
            _mockConfiguration = new Mock<IConfiguration>(); // Added

            // Setup Email Configuration Mocks
            // Email template is now hardcoded, no need to mock configuration for it.
            _mockConfiguration.Setup(c => c["AllowedOrigins"]).Returns("http://localhost:5173");

            _service = new TeamInvitationService(
                _mockInvitationRepository.Object,
                _mockTeamMemberRepository.Object,
                _mockTeamRepository.Object,
                _mockSemesterRepository.Object,
                _mockUserRepository.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object
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
            Assert.Equal("Test Team", result[0].Team.TeamName);
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
    }
}
