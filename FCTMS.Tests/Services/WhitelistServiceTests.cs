using BusinessObjects.Models;
using Moq;
using Repositories;
using Services;
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace FCTMS.Tests.Services
{
    public class WhitelistServiceTests
    {
        private readonly Mock<IWhitelistRepository> _mockWhitelistRepository;
        private readonly WhitelistService _whitelistService;

        public WhitelistServiceTests()
        {
            _mockWhitelistRepository = new Mock<IWhitelistRepository>();
            _whitelistService = new WhitelistService(_mockWhitelistRepository.Object);
        }

        [Fact]
        public async Task UpdateReviewerStatusAsync_ShouldAssignReviewer_WhenValid()
        {
            // Arrange
            int whitelistId = 1;
            var whitelistEntry = new Whitelist
            {
                WhitelistId = whitelistId,
                Email = "test@example.com",
                IsReviewer = false
            };

            _mockWhitelistRepository.Setup(x => x.GetByIdAsync(whitelistId))
                .ReturnsAsync(whitelistEntry);

            // Act
            await _whitelistService.UpdateReviewerStatusAsync(whitelistId, true);

            // Assert
            whitelistEntry.IsReviewer.Should().BeTrue();
            _mockWhitelistRepository.Verify(x => x.UpdateAsync(whitelistEntry), Times.Once);
        }

        [Fact]
        public async Task UpdateReviewerStatusAsync_ShouldUnassignReviewer_WhenValid()
        {
            // Arrange
            int whitelistId = 1;
            var whitelistEntry = new Whitelist
            {
                WhitelistId = whitelistId,
                Email = "test@example.com",
                IsReviewer = true
            };

            _mockWhitelistRepository.Setup(x => x.GetByIdAsync(whitelistId))
                .ReturnsAsync(whitelistEntry);

            // Act
            await _whitelistService.UpdateReviewerStatusAsync(whitelistId, false);

            // Assert
            whitelistEntry.IsReviewer.Should().BeFalse();
            _mockWhitelistRepository.Verify(x => x.UpdateAsync(whitelistEntry), Times.Once);
        }

        [Fact]
        public async Task UpdateReviewerStatusAsync_ShouldThrow_WhenWhitelistNotFound()
        {
            // Arrange
            int whitelistId = 99;
            _mockWhitelistRepository.Setup(x => x.GetByIdAsync(whitelistId))
                .ReturnsAsync((Whitelist?)null);

            // Act
            Func<Task> act = async () => await _whitelistService.UpdateReviewerStatusAsync(whitelistId, true);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Whitelist entry with ID {whitelistId} not found.");
            
            _mockWhitelistRepository.Verify(x => x.UpdateAsync(It.IsAny<Whitelist>()), Times.Never);
        }
    }
}
