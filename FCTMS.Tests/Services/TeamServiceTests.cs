using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Moq;
using Repositories;
using Services;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace FCTMS.Tests.Services
{
    public class TeamServiceTests
    {
        private readonly Mock<ITeamRepository> _mockTeamRepository;
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ICloudinaryHelper> _mockCloudinaryHelper;
        private readonly Mock<IArchivingRepository> _mockArchivingRepository;
        private readonly TeamService _teamService;

        public TeamServiceTests()
        {
            _mockTeamRepository = new Mock<ITeamRepository>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockCloudinaryHelper = new Mock<ICloudinaryHelper>();
            _mockArchivingRepository = new Mock<IArchivingRepository>();

            _teamService = new TeamService(
                _mockTeamRepository.Object,
                _mockSemesterRepository.Object,
                _mockUserRepository.Object,
                _mockCloudinaryHelper.Object,
                _mockArchivingRepository.Object
            );
        }

        [Fact]
        public async Task DisbandTeamAsync_ArchivesTeam_WhenLeaderRequests()
        {
            // Arrange
            int teamId = 1;
            int leaderId = 100;
            var team = new Team
            {
                TeamId = teamId,
                LeaderId = leaderId,
                Status = "Insufficient",
                Teammembers = new List<Teammember>() // Empty list is fine for this test, ArchivingRepo handles serialization
            };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(team);
            _mockArchivingRepository.Setup(r => r.ArchiveTeamAsync(It.IsAny<Team>()))
                .Returns(Task.CompletedTask);
            // Act
            var result = await _teamService.DisbandTeamAsync(teamId, leaderId);
            // Assert
            Assert.True(result);
            _mockArchivingRepository.Verify(r => r.ArchiveTeamAsync(team), Times.Once);
            _mockTeamRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public async Task DisbandTeamAsync_ReturnsFalse_WhenTeamNotFound()
        {
            // Arrange
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Team)null);
            // Act
            var result = await _teamService.DisbandTeamAsync(1, 1);
            // Assert
            Assert.False(result);
            _mockArchivingRepository.Verify(r => r.ArchiveTeamAsync(It.IsAny<Team>()), Times.Never);
        }
        [Fact]
        public async Task DisbandTeamAsync_ThrowsException_WhenNotLeader()
        {
            // Arrange
            var team = new Team { TeamId = 1, LeaderId = 999 };
            _mockTeamRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(team);
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _teamService.DisbandTeamAsync(1, 1));
            _mockArchivingRepository.Verify(r => r.ArchiveTeamAsync(It.IsAny<Team>()), Times.Never);
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
                Teammembers = new List<Teammember>() // Prevent NullReferenceException in MapToDTO
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
    }
}
