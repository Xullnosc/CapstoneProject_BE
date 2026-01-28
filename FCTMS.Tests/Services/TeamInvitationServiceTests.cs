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

namespace FCTMS.Tests.Services
{
    public class TeamInvitationServiceTests
    {
        private readonly Mock<ITeamInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ITeamMemberRepository> _mockTeamMemberRepository;
        private readonly Mock<ITeamRepository> _mockTeamRepository;
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly TeamInvitationService _service;

        public TeamInvitationServiceTests()
        {
            _mockInvitationRepository = new Mock<ITeamInvitationRepository>();
            _mockTeamMemberRepository = new Mock<ITeamMemberRepository>();
            _mockTeamRepository = new Mock<ITeamRepository>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockUserRepository = new Mock<IUserRepository>();

            _service = new TeamInvitationService(
                _mockInvitationRepository.Object,
                _mockTeamMemberRepository.Object,
                _mockTeamRepository.Object,
                _mockSemesterRepository.Object,
                _mockUserRepository.Object
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
            _mockInvitationRepository.Verify(r => r.GetByStudentIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
