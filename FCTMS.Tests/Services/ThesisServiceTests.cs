using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Repositories;
using Services;
using Services.Helpers;
using Services.Mappings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FCTMS.Tests.Services
{
    public class ThesisServiceTests
    {
        private readonly Mock<IThesisRepository> _mockThesisRepository;
        private readonly Mock<ITeamRepository> _mockTeamRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ICloudinaryHelper> _mockCloudinaryHelper;
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ThesisService _thesisService;

        public ThesisServiceTests()
        {
            _mockThesisRepository = new Mock<IThesisRepository>();
            _mockTeamRepository = new Mock<ITeamRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockCloudinaryHelper = new Mock<ICloudinaryHelper>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockMapper = new Mock<IMapper>();

            _thesisService = new ThesisService(
                _mockThesisRepository.Object,
                _mockTeamRepository.Object,
                _mockUserRepository.Object,
                _mockCloudinaryHelper.Object,
                _mockSemesterRepository.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task GetMyThesesAsync_ShouldReturnMappedDtos_WhenUserExists()
        {
            // Arrange
            string email = "student@fpt.edu.vn";
            int userId = 1;
            var user = new User { UserId = userId, Email = email };
            var theses = new List<Thesis>
            {
                new Thesis { ThesisId = Guid.NewGuid(), Title = "Thesis 1", UserId = userId },
                new Thesis { ThesisId = Guid.NewGuid(), Title = "Thesis 2", UserId = userId }
            };

            var currentSemester = new Semester { SemesterId = 1 };
            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockSemesterRepository.Setup(x => x.GetCurrentSemesterAsync()).ReturnsAsync(currentSemester);
            _mockTeamRepository.Setup(x => x.GetActiveTeamByStudentIdAsync(userId)).ReturnsAsync((Team?)null);
            _mockThesisRepository.Setup(x => x.GetThesesByUserIdsAsync(It.IsAny<IEnumerable<int>>(), currentSemester.SemesterId)).ReturnsAsync(theses);
            _mockMapper.Setup(m => m.Map<IEnumerable<ThesisDTO>>(theses)).Returns(new List<ThesisDTO> 
            { 
                new ThesisDTO { Title = "Thesis 1" }, 
                new ThesisDTO { Title = "Thesis 2" } 
            });

            // Act
            var result = await _thesisService.GetMyThesesAsync(email);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(t => t.Title == "Thesis 1");
        }

        [Fact]
        public async Task GetMyThesesAsync_ShouldThrowUnauthorized_WhenUserNotFound()
        {
            // Arrange
            _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _thesisService.GetMyThesesAsync("unknown@fpt.edu.vn");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("User not found.");
        }

        [Fact]
        public async Task GetThesisDetailAsync_ShouldReturnNull_WhenThesisNotFound()
        {
            // Arrange
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(It.IsAny<string>()))
                .ReturnsAsync((Thesis?)null);

            // Act
            var result = await _thesisService.GetThesisDetailAsync("invalid_id");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetThesisDetailAsync_ShouldReturnDto_WhenFound()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            var thesis = new Thesis
            {
                ThesisId = Guid.Parse(thesisId),
                Title = "Test Thesis",
                ThesisHistories = new List<ThesisHistory>
                {
                    new ThesisHistory { Id = 1, VersionNumber = 1 }
                }
            };
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(thesis);
            _mockMapper.Setup(m => m.Map<ThesisDTO>(thesis)).Returns(new ThesisDTO 
            { 
                ThesisId = thesisId, 
                Histories = new List<ThesisHistoryDTO> { new ThesisHistoryDTO { Id = 1 } } 
            });

            // Act
            var result = await _thesisService.GetThesisDetailAsync(thesisId);

            // Assert
            result.Should().NotBeNull();
            result!.ThesisId.Should().Be(thesisId);
            result.Histories.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetFilteredThesesAsync_ShouldReturnMappedDtos()
        {
            // Arrange
            var theses = new List<Thesis>
            {
                new Thesis { ThesisId = Guid.NewGuid(), Status = "Reviewing" }
            };
            _mockThesisRepository.Setup(x => x.GetAllThesesFilteredAsync("Reviewing", null)).ReturnsAsync(theses);
            _mockMapper.Setup(m => m.Map<IEnumerable<ThesisDTO>>(theses)).Returns(new List<ThesisDTO> 
            { 
                new ThesisDTO { ThesisId = "1", Status = "Reviewing" } 
            });

            // Act
            var result = await _thesisService.GetFilteredThesesAsync("Reviewing", null);

            // Assert
            result.Should().HaveCount(1);
            result.First().Status.Should().Be("Reviewing");
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldThrowException_WhenUserIsNotOwner()
        {
            // Arrange
            var req = new UpdateThesisDTO { Title = "New Title" };
            var reqEmail = "other@fpt.edu.vn";
            var dbUser = new User { UserId = 2, Email = reqEmail };
            var dbThesis = new Thesis { ThesisId = Guid.NewGuid(), UserId = 1 }; // Owned by user 1

            _mockUserRepository.Setup(x => x.GetByEmailAsync(reqEmail)).ReturnsAsync(dbUser);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync("1")).ReturnsAsync(dbThesis);

            // Act
            Func<Task> act = async () => await _thesisService.UpdateThesisAsync("1", req, reqEmail);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not authorized to update this thesis.");
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldUpdateTitleAndDescription_WhenNoFileProvided()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;
            var req = new UpdateThesisDTO { Title = "Updated Title" };
            
            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis { ThesisId = Guid.Parse(thesisId), UserId = ownerId, Title = "Old Title", ThesisHistories = new List<ThesisHistory>() };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(thesis);
            // Setup so reloading returns the updated object
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(thesis);
            _mockMapper.Setup(m => m.Map<ThesisDTO>(thesis)).Returns(new ThesisDTO { Title = "Updated Title" });

            // Act
            var result = await _thesisService.UpdateThesisAsync(thesisId, req, email);

            // Assert
            thesis.Title.Should().Be("Updated Title");
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
            _mockThesisRepository.Verify(x => x.AddThesisHistoryAsync(It.IsAny<ThesisHistory>()), Times.Never); // No file means no history created
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldUploadFileAndCreateHistory_WhenFileProvided()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;

            var mockFile = new Mock<IFormFile>();
            var req = new UpdateThesisDTO { File = mockFile.Object, Note = "Version 2" };
            
            var user = new User { UserId = ownerId, Email = email };
            // Simulate thesis already having 1 history
            var thesis = new Thesis 
            { 
                ThesisId = Guid.Parse(thesisId),
                UserId = ownerId, 
                FileUrl = "old_url",
                ThesisHistories = new List<ThesisHistory> 
                { 
                    new ThesisHistory { VersionNumber = 1 } 
                }
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(thesis);
            _mockCloudinaryHelper.Setup(x => x.UploadFileAsync(mockFile.Object)).ReturnsAsync("new_secure_url");
            _mockMapper.Setup(m => m.Map<ThesisDTO>(thesis)).Returns(new ThesisDTO { FileUrl = "new_secure_url" });

            // Act
            var result = await _thesisService.UpdateThesisAsync(thesisId, req, email);

            // Assert
            thesis.FileUrl.Should().Be("new_secure_url");
            _mockCloudinaryHelper.Verify(x => x.UploadFileAsync(mockFile.Object), Times.Once);
            
            _mockThesisRepository.Verify(x => x.AddThesisHistoryAsync(It.Is<ThesisHistory>(h => 
                h.ThesisId == Guid.Parse(thesisId) &&
                h.FileUrl == "new_secure_url" &&
                h.VersionNumber == 2 && // Max(1) + 1
                h.Note == "Version 2" &&
                h.UploadedBy == ownerId
            )), Times.Once);

            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
        }

        [Fact]
        public async Task UpdateThesisStatusAsync_ShouldUpdateStatus_WhenThesisExists()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            string newStatus = "Published";
            var thesis = new Thesis { ThesisId = Guid.Parse(thesisId), Status = "Reviewing", UpdateDate = null };

            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);

            // Act
            await _thesisService.UpdateThesisStatusAsync(thesisId, newStatus);

            // Assert
            thesis.Status.Should().Be(newStatus);
            thesis.UpdateDate.Should().NotBeNull();
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
        }

        [Fact]
        public async Task UpdateThesisStatusAsync_ShouldThrow_WhenThesisNotFound()
        {
            // Arrange
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(It.IsAny<string>())).ReturnsAsync((Thesis?)null);

            // Act
            Func<Task> act = async () => await _thesisService.UpdateThesisStatusAsync("invalid-id", "Published");

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Thesis not found");
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(It.IsAny<Thesis>()), Times.Never);
        }

        [Fact]
        public async Task GetMyThesesAsync_ShouldIncludeLeaderTheses_WhenUserIsTeamMember()
        {
            // Arrange
            string studentEmail = "member@fpt.edu.vn";
            int studentId = 2;
            int leaderId = 1;
            var user = new User { UserId = studentId, Email = studentEmail };
            var team = new Team { TeamId = 10, LeaderId = leaderId };
            
            var theses = new List<Thesis>
            {
                new Thesis { ThesisId = Guid.NewGuid(), Title = "Leader Thesis", UserId = leaderId },
                new Thesis { ThesisId = Guid.NewGuid(), Title = "Member Thesis", UserId = studentId }
            };

            var currentSemester = new Semester { SemesterId = 1 };
            _mockUserRepository.Setup(x => x.GetByEmailAsync(studentEmail)).ReturnsAsync(user);
            _mockSemesterRepository.Setup(x => x.GetCurrentSemesterAsync()).ReturnsAsync(currentSemester);
            _mockTeamRepository.Setup(x => x.GetActiveTeamByStudentIdAsync(studentId)).ReturnsAsync(team);
            
            // Should call GetThesesByUserIdsAsync with [2, 1] and semesterId 1
            _mockThesisRepository.Setup(x => x.GetThesesByUserIdsAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(studentId) && ids.Contains(leaderId)), currentSemester.SemesterId))
                .ReturnsAsync(theses);

            _mockMapper.Setup(m => m.Map<IEnumerable<ThesisDTO>>(It.IsAny<IEnumerable<Thesis>>())).Returns(new List<ThesisDTO> 
            { 
                new ThesisDTO { Title = "Leader Thesis" }, 
                new ThesisDTO { Title = "Member Thesis" } 
            });

            // Act
            var result = await _thesisService.GetMyThesesAsync(studentEmail);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(t => t.Title == "Leader Thesis");
            result.Should().Contain(t => t.Title == "Member Thesis");
            
            _mockThesisRepository.Verify(x => x.GetThesesByUserIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<int?>()), Times.Once);
            _mockThesisRepository.Verify(x => x.GetThesesByUserIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldSyncTitleWithFileName_WhenFileProvidedAndNoTitle()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("New_Thesis_File.docx");
            mockFile.Setup(f => f.Length).Returns(100);
            
            var req = new UpdateThesisDTO { File = mockFile.Object, Title = "" }; // Empty title
            
            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis { ThesisId = Guid.Parse(thesisId), UserId = ownerId, Title = "Old Title", ThesisHistories = new List<ThesisHistory>() };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(thesis);
            _mockCloudinaryHelper.Setup(x => x.UploadFileAsync(mockFile.Object)).ReturnsAsync("new_url");
            _mockMapper.Setup(m => m.Map<ThesisDTO>(It.IsAny<Thesis>())).Returns(new ThesisDTO { Title = "New_Thesis_File" });

            // Act
            var result = await _thesisService.UpdateThesisAsync(thesisId, req, email);

            // Assert
            thesis.Title.Should().Be("New_Thesis_File");
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
        }

        [Fact]
        public async Task CancelThesisAsync_ShouldUpdateStatusToCancelled_WhenValid()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;
            
            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis { ThesisId = Guid.Parse(thesisId), UserId = ownerId, Status = "Reviewing" };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(thesis);
            
            var expectedDto = new ThesisDTO { ThesisId = thesisId, Status = "Cancelled" };
            _mockMapper.Setup(m => m.Map<ThesisDTO>(It.IsAny<Thesis>())).Returns(expectedDto);

            // Act
            var result = await _thesisService.CancelThesisAsync(thesisId, email);

            // Assert
            thesis.Status.Should().Be("Cancelled");
            thesis.UpdateDate.Should().NotBeNull();
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
            result.Status.Should().Be("Cancelled");
        }

        [Fact]
        public async Task CancelThesisAsync_ShouldThrowUnauthorized_WhenUserIsNotOwner()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            string reqEmail = "other@fpt.edu.vn";
            var dbUser = new User { UserId = 2, Email = reqEmail };
            var dbThesis = new Thesis { ThesisId = Guid.Parse(thesisId), UserId = 1 }; // Owned by user 1

            _mockUserRepository.Setup(x => x.GetByEmailAsync(reqEmail)).ReturnsAsync(dbUser);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(dbThesis);

            // Act
            Func<Task> act = async () => await _thesisService.CancelThesisAsync(thesisId, reqEmail);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not authorized to cancel this thesis.");
        }

        [Fact]
        public async Task CancelThesisAsync_ShouldThrowInvalidOperation_WhenStatusIsPublished()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;
            
            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis { ThesisId = Guid.Parse(thesisId), UserId = ownerId, Status = "Published" };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(thesis);

            // Act
            Func<Task> act = async () => await _thesisService.CancelThesisAsync(thesisId, email);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot cancel a thesis that is 'Published'.");
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldSetStatus_OnMentorInviting()
        {
            // Arrange
            string email = "lecturer@fpt.edu.vn";
            var user = new User { UserId = 10, Email = email, Role = new Role { RoleName = "Lecturer" } };
            var currentSemester = new Semester { SemesterId = 1 };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("thesis.docx");

            var req = new ProposeThesisDTO { Title = "My Thesis", File = mockFile.Object };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesesByUserIdAsync(user.UserId)).ReturnsAsync(new List<Thesis>());
            _mockCloudinaryHelper.Setup(x => x.UploadFileAsync(mockFile.Object)).ReturnsAsync("https://cloudinary.com/file.docx");
            _mockSemesterRepository.Setup(x => x.GetCurrentSemesterAsync()).ReturnsAsync(currentSemester);

            Thesis? capturedThesis = null;
            _mockThesisRepository
                .Setup(x => x.CreateThesisAsync(It.IsAny<Thesis>()))
                .Callback<Thesis>(t => capturedThesis = t)
                .ReturnsAsync((Thesis t) => t);

            // Act
            var result = await _thesisService.ProposeThesisAsync(req, email);

            // Assert
            capturedThesis.Should().NotBeNull();
            capturedThesis!.Status.Should().Be("On Mentor Inviting");
        }

        [Fact]
        public async Task CancelThesisAsync_ShouldSucceed_WhenStatusIsOnMentorInviting()
        {
            // Arrange
            string thesisId = Guid.NewGuid().ToString();
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;

            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis { ThesisId = Guid.Parse(thesisId), UserId = ownerId, Status = "On Mentor Inviting" };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId)).ReturnsAsync(thesis);

            var expectedDto = new ThesisDTO { ThesisId = thesisId, Status = "Cancelled" };
            _mockMapper.Setup(m => m.Map<ThesisDTO>(It.IsAny<Thesis>())).Returns(expectedDto);

            // Act
            var result = await _thesisService.CancelThesisAsync(thesisId, email);

            // Assert
            thesis.Status.Should().Be("Cancelled");
            thesis.UpdateDate.Should().NotBeNull();
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
            result.Status.Should().Be("Cancelled");
        }
    }
}

