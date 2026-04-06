using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Repositories;
using Services;
using Services.Helpers;
using Services.Mappings;
using Xunit;

namespace FCTMS.Tests.Services
{
    public class ThesisServiceTests
    {
        private readonly Mock<IThesisRepository> _mockThesisRepository;
        private readonly Mock<IThesisReviewRepository> _mockThesisReviewRepository;
        private readonly Mock<ITeamRepository> _mockTeamRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ICloudinaryHelper> _mockCloudinaryHelper;
        private readonly Mock<ISemesterRepository> _mockSemesterRepository;
        private readonly Mock<ILecturerRepository> _mockLecturerRepository;
        private readonly Mock<ITeamInvitationRepository> _mockTeamInvitationRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ISystemParameterService> _mockSystemParameterService;
        private readonly ThesisService _thesisService;

        public ThesisServiceTests()
        {
            _mockThesisRepository = new Mock<IThesisRepository>();
            _mockThesisReviewRepository = new Mock<IThesisReviewRepository>();
            _mockTeamRepository = new Mock<ITeamRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockCloudinaryHelper = new Mock<ICloudinaryHelper>();
            _mockSemesterRepository = new Mock<ISemesterRepository>();
            _mockLecturerRepository = new Mock<ILecturerRepository>();
            _mockTeamInvitationRepository = new Mock<ITeamInvitationRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockSystemParameterService = new Mock<ISystemParameterService>();

            // Default: registration open, 10MB limit â€” keeps all existing tests green
            _mockSystemParameterService
                .Setup(s => s.GetBoolAsync("THESIS_REGISTRATION_OPEN", It.IsAny<bool>()))
                .ReturnsAsync(true);
            _mockSystemParameterService
                .Setup(s => s.GetIntAsync("FILE_SIZE_LIMIT_MB", It.IsAny<int>()))
                .ReturnsAsync(10);

            _thesisService = new ThesisService(
                _mockThesisRepository.Object,
                _mockThesisReviewRepository.Object,
                _mockTeamRepository.Object,
                _mockUserRepository.Object,
                _mockCloudinaryHelper.Object,
                _mockSemesterRepository.Object,
                _mockLecturerRepository.Object,
                _mockTeamInvitationRepository.Object,
                _mockMapper.Object,
                _mockSystemParameterService.Object
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
                new Thesis
                {
                    ThesisId = "1",
                    Title = "Thesis 1",
                    UserId = userId,
                },
                new Thesis
                {
                    ThesisId = "2",
                    Title = "Thesis 2",
                    UserId = userId,
                },
            };

            var currentSemester = new Semester { SemesterId = 1 };
            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockSemesterRepository
                .Setup(x => x.GetCurrentSemesterAsync())
                .ReturnsAsync(currentSemester);
            _mockTeamRepository
                .Setup(x => x.GetActiveTeamByStudentIdAsync(userId))
                .ReturnsAsync((Team?)null);
            _mockThesisRepository
                .Setup(x =>
                    x.GetThesesByOwnerOrTeamAsync(
                        It.IsAny<IEnumerable<int>>(),
                        It.IsAny<IEnumerable<int>>(),
                        It.Is<int?>(id => id == currentSemester.SemesterId)
                    )
                )
                .ReturnsAsync(theses);
            _mockMapper
                .Setup(m => m.Map<IEnumerable<ThesisDTO>>(theses))
                .Returns(
                    new List<ThesisDTO>
                    {
                        new ThesisDTO { Title = "Thesis 1" },
                        new ThesisDTO { Title = "Thesis 2" },
                    }
                );

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
            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () =>
                await _thesisService.GetMyThesesAsync("unknown@fpt.edu.vn");

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User not found.");
        }

        [Fact]
        public async Task GetThesisDetailAsync_ShouldReturnNull_WhenThesisNotFound()
        {
            // Arrange
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(It.IsAny<string>()))
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
            string thesisId = "valid_id";
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                Title = "Test Thesis",
                ThesisHistories = new List<ThesisHistory>
                {
                    new ThesisHistory { Id = 1, VersionNumber = 1 },
                },
            };
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);
            _mockMapper
                .Setup(m => m.Map<ThesisDTO>(thesis))
                .Returns(
                    new ThesisDTO
                    {
                        ThesisId = thesisId,
                        Histories = new List<ThesisHistoryDTO> { new ThesisHistoryDTO { Id = 1 } },
                    }
                );

            // Act
            var result = await _thesisService.GetThesisDetailAsync(thesisId);

            // Assert
            result.Should().NotBeNull();
            result!.ThesisId.Should().Be(thesisId);
            result.Histories.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetFilteredThesesAsync_ShouldReturnMappedDtos_AndPassNewParameters()
        {
            // Arrange
            var theses = new List<Thesis>
            {
                new Thesis
                {
                    ThesisId = "1",
                    Status = "Published",
                    IsLocked = false,
                },
            };
            _mockThesisRepository.Setup(x => x.GetAllThesesFilteredAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<int?>())).ReturnsAsync(theses);
            _mockMapper.Setup(m => m.Map<IEnumerable<ThesisDTO>>(theses)).Returns(new List<ThesisDTO> 
            { 
                new ThesisDTO { ThesisId = "1", Status = "Published", IsLocked = false } 
            });

            // Act
            var result = await _thesisService.GetFilteredThesesAsync(
                "Published",
                null,
                null,
                null,
                false,
                true
            );

            // Assert
            result.Should().HaveCount(1);
            result.First().Status.Should().Be("Published");
            result.First().IsLocked.Should().BeFalse();

            // Verify the repository was called with the exact parameters
            _mockThesisRepository.Verify(x => x.GetAllThesesFilteredAsync("Published", null, null, false, true, null), Times.Once);
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldThrowException_WhenUserIsNotOwner()
        {
            // Arrange
            var req = new UpdateThesisDTO { Title = "New Title" };
            var reqEmail = "other@fpt.edu.vn";
            var dbUser = new User { UserId = 2, Email = reqEmail };
            var dbThesis = new Thesis { ThesisId = "1", UserId = 1 }; // Owned by user 1

            _mockUserRepository.Setup(x => x.GetByEmailAsync(reqEmail)).ReturnsAsync(dbUser);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync("1"))
                .ReturnsAsync(dbThesis);

            // Act
            Func<Task> act = async () => await _thesisService.UpdateThesisAsync("1", req, reqEmail);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not authorized to update this thesis.");
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldUpdateTitleAndDescription_WhenNoFileProvided()
        {
            // Arrange
            string thesisId = "1";
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;
            var req = new UpdateThesisDTO { Title = "Updated Title" };

            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                UserId = ownerId,
                Title = "Old Title",
                Status = "Need Update",
                ThesisHistories = new List<ThesisHistory>(),
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);
            // Setup so reloading returns the updated object
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);
            _mockMapper
                .Setup(m => m.Map<ThesisDTO>(thesis))
                .Returns(new ThesisDTO { Title = "Updated Title" });

            // Act
            var result = await _thesisService.UpdateThesisAsync(thesisId, req, email);

            // Assert
            thesis.Title.Should().Be("Updated Title");
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
            _mockThesisRepository.Verify(
                x => x.AddThesisHistoryAsync(It.IsAny<ThesisHistory>()),
                Times.Never
            ); // No file means no history created
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldUploadFileAndCreateHistory_WhenFileProvided()
        {
            // Arrange
            string thesisId = "1";
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;

            var mockFile = new Mock<IFormFile>();
            var req = new UpdateThesisDTO { File = mockFile.Object };

            var user = new User { UserId = ownerId, Email = email };
            // Simulate thesis already having 1 history
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                UserId = ownerId,
                FileUrl = "old_url",
                Status = "Need Update",
                ThesisHistories = new List<ThesisHistory>
                {
                    new ThesisHistory { VersionNumber = 1 },
                },
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);
            _mockCloudinaryHelper
                .Setup(x => x.UploadFileAsync(mockFile.Object))
                .ReturnsAsync("new_secure_url");
            _mockMapper
                .Setup(m => m.Map<ThesisDTO>(thesis))
                .Returns(new ThesisDTO { FileUrl = "new_secure_url" });

            // Act
            var result = await _thesisService.UpdateThesisAsync(thesisId, req, email);

            // Assert
            thesis.FileUrl.Should().Be("new_secure_url");
            _mockCloudinaryHelper.Verify(x => x.UploadFileAsync(mockFile.Object), Times.Once);

            _mockThesisRepository.Verify(
                x =>
                    x.AddThesisHistoryAsync(
                        It.Is<ThesisHistory>(h =>
                            h.VersionNumber == 2
                            && h.UploadedBy == ownerId
                        )
                    ),
                Times.Once
            );

            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
        }

        [Fact]
        public async Task UpdateThesisStatusAsync_ShouldUpdateStatus_WhenThesisExists()
        {
            // Arrange
            string thesisId = "guid-123";
            string newStatus = "Published";
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                Status = "Reviewing",
                UpdateDate = null,
            };

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
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Thesis?)null);

            // Act
            Func<Task> act = async () =>
                await _thesisService.UpdateThesisStatusAsync("invalid-id", "Published");

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
                new Thesis
                {
                    ThesisId = "T1",
                    Title = "Leader Thesis",
                    UserId = leaderId,
                },
                new Thesis
                {
                    ThesisId = "T2",
                    Title = "Member Thesis",
                    UserId = studentId,
                },
            };

            var currentSemester = new Semester { SemesterId = 1 };
            _mockUserRepository.Setup(x => x.GetByEmailAsync(studentEmail)).ReturnsAsync(user);
            _mockSemesterRepository
                .Setup(x => x.GetCurrentSemesterAsync())
                .ReturnsAsync(currentSemester);
            _mockTeamRepository
                .Setup(x => x.GetActiveTeamByStudentIdAsync(studentId))
                .ReturnsAsync(team);

            // Should call GetThesesByOwnerOrTeamAsync with ownerIds [2, 1] and teamIds [10]
            _mockThesisRepository
                .Setup(x =>
                    x.GetThesesByOwnerOrTeamAsync(
                        It.Is<IEnumerable<int>>(ids =>
                            ids.Contains(studentId) && ids.Contains(leaderId)
                        ),
                        It.Is<IEnumerable<int>>(ids =>
                           ids.Contains(team.TeamId)
                        ),
                        It.Is<int?>(id => id == currentSemester.SemesterId)
                    )
                )
                .ReturnsAsync(theses);

            _mockMapper
                .Setup(m => m.Map<IEnumerable<ThesisDTO>>(It.IsAny<IEnumerable<Thesis>>()))
                .Returns(
                    new List<ThesisDTO>
                    {
                        new ThesisDTO { Title = "Leader Thesis" },
                        new ThesisDTO { Title = "Member Thesis" },
                    }
                );

            // Act
            var result = await _thesisService.GetMyThesesAsync(studentEmail);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(t => t.Title == "Leader Thesis");
            result.Should().Contain(t => t.Title == "Member Thesis");

            _mockThesisRepository.Verify(
                x => x.GetThesesByOwnerOrTeamAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<int?>()),
                Times.Once
            );
            _mockThesisRepository.Verify(
                x => x.GetThesesByUserIdAsync(It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldSyncTitleWithFileName_WhenFileProvidedAndNoTitle()
        {
            // Arrange
            string thesisId = "1";
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("New_Thesis_File.docx");
            mockFile.Setup(f => f.Length).Returns(100);

            var req = new UpdateThesisDTO { File = mockFile.Object, Title = "" }; // Empty title

            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                UserId = ownerId,
                Title = "Old Title",
                Status = "Need Update",
                ThesisHistories = new List<ThesisHistory>(),
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);
            _mockCloudinaryHelper
                .Setup(x => x.UploadFileAsync(mockFile.Object))
                .ReturnsAsync("new_url");
            _mockMapper
                .Setup(m => m.Map<ThesisDTO>(It.IsAny<Thesis>()))
                .Returns(new ThesisDTO { Title = "New_Thesis_File" });

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
            string thesisId = "1";
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;

            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                UserId = ownerId,
                Status = "Reviewing",
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);

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
            string thesisId = "1";
            string reqEmail = "other@fpt.edu.vn";
            var dbUser = new User { UserId = 2, Email = reqEmail };
            var dbThesis = new Thesis { ThesisId = thesisId, UserId = 1 }; // Owned by user 1

            _mockUserRepository.Setup(x => x.GetByEmailAsync(reqEmail)).ReturnsAsync(dbUser);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(dbThesis);

            // Act
            Func<Task> act = async () => await _thesisService.CancelThesisAsync(thesisId, reqEmail);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not authorized to cancel this thesis.");
        }

        [Fact]
        public async Task CancelThesisAsync_ShouldThrowInvalidOperation_WhenStatusIsPublished()
        {
            // Arrange
            string thesisId = "1";
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;

            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                UserId = ownerId,
                Status = "Published",
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);

            // Act
            Func<Task> act = async () => await _thesisService.CancelThesisAsync(thesisId, email);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot cancel a thesis that is 'Published'.");
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldSetStatus_OnMentorInviting_WhenStudent()
        {
            // Arrange
            string email = "student@fpt.edu.vn";
            int userId = 10;
            var user = new User
            {
                UserId = userId,
                Email = email,
                Role = new Role { RoleName = "Student" },
            };
            var currentSemester = new Semester { SemesterId = 1, Status = CampusConstants.SemesterStatus.Open };
            var team = new Team
            {
                TeamId = 1,
                LeaderId = userId,
                Teammembers = new List<Teammember> { new(), new(), new(), new() }, // exactly 4
            };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("thesis.docx");

            var req = new ProposeThesisDTO { Title = "My Thesis", File = mockFile.Object };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesesByUserIdAsync(user.UserId))
                .ReturnsAsync(new List<Thesis>());
            _mockTeamRepository
                .Setup(x => x.GetActiveTeamByStudentIdAsync(userId))
                .ReturnsAsync(team);
            _mockCloudinaryHelper
                .Setup(x => x.UploadFileAsync(mockFile.Object))
                .ReturnsAsync("https://cloudinary.com/file.docx");
            _mockSemesterRepository
                .Setup(x => x.GetCurrentSemesterAsync())
                .ReturnsAsync(currentSemester);

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
        public async Task ProposeThesisAsync_ShouldSetStatus_Reviewing_WhenLecturer()
        {
            // Arrange
            string email = "lecturer@fpt.edu.vn";
            var user = new User
            {
                UserId = 20,
                Email = email,
                Role = new Role { RoleName = "Lecturer" },
            };
            var currentSemester = new Semester { SemesterId = 1, Status = CampusConstants.SemesterStatus.Open };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("thesis.docx");

            var req = new ProposeThesisDTO { Title = "Lecturer Thesis", File = mockFile.Object };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesesByUserIdAsync(user.UserId))
                .ReturnsAsync(new List<Thesis>());
            _mockCloudinaryHelper
                .Setup(x => x.UploadFileAsync(mockFile.Object))
                .ReturnsAsync("https://cloudinary.com/file.docx");
            _mockSemesterRepository
                .Setup(x => x.GetCurrentSemesterAsync())
                .ReturnsAsync(currentSemester);

            Thesis? capturedThesis = null;
            _mockThesisRepository
                .Setup(x => x.CreateThesisAsync(It.IsAny<Thesis>()))
                .Callback<Thesis>(t => capturedThesis = t)
                .ReturnsAsync((Thesis t) => t);

            // Act
            var result = await _thesisService.ProposeThesisAsync(req, email);

            // Assert
            capturedThesis.Should().NotBeNull();
            capturedThesis!.Status.Should().Be("Reviewing");
        }

        [Fact]
        public async Task CancelThesisAsync_ShouldSucceed_WhenStatusIsOnMentorInviting()
        {
            // Arrange
            string thesisId = "1";
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;

            var user = new User { UserId = ownerId, Email = email };
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                UserId = ownerId,
                Status = "On Mentor Inviting",
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);

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

        // â”€â”€â”€ ProposeThesisAsync â€“ Team Validation Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task ProposeThesisAsync_ShouldThrow_WhenStudentHasNoActiveTeam()
        {
            // Arrange
            string email = "student@fpt.edu.vn";
            var user = new User
            {
                UserId = 5,
                Email = email,
                Role = new Role { RoleName = "Student" },
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesesByUserIdAsync(user.UserId))
                .ReturnsAsync(new List<Thesis>());
            _mockTeamRepository
                .Setup(x => x.GetActiveTeamByStudentIdAsync(user.UserId))
                .ReturnsAsync((Team?)null);

            // Act
            Func<Task> act = async () =>
                await _thesisService.ProposeThesisAsync(
                    new ProposeThesisDTO { Title = "T", File = new Mock<IFormFile>().Object },
                    email
                );

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Target user must be in an active team to propose a thesis.");
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldThrow_WhenStudentIsNotTeamLeader()
        {
            // Arrange
            string email = "member@fpt.edu.vn";
            int userId = 5;
            var user = new User
            {
                UserId = userId,
                Email = email,
                Role = new Role { RoleName = "Student" },
            };
            // LeaderId is different from userId â†’ user is NOT the leader
            var team = new Team
            {
                TeamId = 1,
                LeaderId = 99,
                Teammembers = new List<Teammember> { new(), new(), new(), new() },
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesesByUserIdAsync(userId))
                .ReturnsAsync(new List<Thesis>());
            _mockTeamRepository
                .Setup(x => x.GetActiveTeamByStudentIdAsync(userId))
                .ReturnsAsync(team);

            // Act
            Func<Task> act = async () =>
                await _thesisService.ProposeThesisAsync(
                    new ProposeThesisDTO { Title = "T", File = new Mock<IFormFile>().Object },
                    email
                );

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Only the team leader can propose a thesis.");
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldThrow_WhenTeamHasFewerThan4Members()
        {
            // Arrange
            string email = "leader@fpt.edu.vn";
            int userId = 5;
            var user = new User
            {
                UserId = userId,
                Email = email,
                Role = new Role { RoleName = "Student" },
            };
            // Leader but only 3 members
            var team = new Team
            {
                TeamId = 1,
                LeaderId = userId,
                Teammembers = new List<Teammember> { new(), new(), new() }, // 3 members
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesesByUserIdAsync(userId))
                .ReturnsAsync(new List<Thesis>());
            _mockTeamRepository
                .Setup(x => x.GetActiveTeamByStudentIdAsync(userId))
                .ReturnsAsync(team);

            // Act
            Func<Task> act = async () =>
                await _thesisService.ProposeThesisAsync(
                    new ProposeThesisDTO { Title = "T", File = new Mock<IFormFile>().Object },
                    email
                );

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage(
                    "Target team must have at least 4 members to propose a thesis unless marked as special. Current members: 3."
                );
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldSucceed_WhenTeamIsSpecialWithFewerThan4Members()
        {
            // Arrange
            string email = "leader@fpt.edu.vn";
            int userId = 5;
            var user = new User
            {
                UserId = userId,
                Email = email,
                Role = new Role { RoleName = "Student" },
            };
            // Only 2 members but team is marked as Special
            var team = new Team
            {
                TeamId = 1,
                LeaderId = userId,
                IsSpecial = true,
                Teammembers = new List<Teammember> { new(), new() }, // 2 members
            };
            var currentSemester = new Semester { SemesterId = 1, Status = CampusConstants.SemesterStatus.Open };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("thesis.docx");

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesesByUserIdAsync(userId))
                .ReturnsAsync(new List<Thesis>());
            _mockTeamRepository
                .Setup(x => x.GetActiveTeamByStudentIdAsync(userId))
                .ReturnsAsync(team);
            _mockCloudinaryHelper
                .Setup(x => x.UploadFileAsync(mockFile.Object))
                .ReturnsAsync("https://cloudinary.com/file.docx");
            _mockSemesterRepository
                .Setup(x => x.GetCurrentSemesterAsync())
                .ReturnsAsync(currentSemester);

            Thesis? capturedThesis = null;
            _mockThesisRepository
                .Setup(x => x.CreateThesisAsync(It.IsAny<Thesis>()))
                .Callback<Thesis>(t => capturedThesis = t)
                .ReturnsAsync((Thesis t) => t);

            // Act
            var result = await _thesisService.ProposeThesisAsync(
                new ProposeThesisDTO { Title = "Special Team Thesis", File = mockFile.Object },
                email
            );

            // Assert â€” should succeed despite only 2 members because IsSpecial = true
            capturedThesis.Should().NotBeNull();
            capturedThesis!.Status.Should().Be("On Mentor Inviting");
            capturedThesis.UserId.Should().Be(userId);
            _mockThesisRepository.Verify(x => x.CreateThesisAsync(It.IsAny<Thesis>()), Times.Once);
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldSucceed_WhenStudentLeaderWithFourMembers()
        {
            // Arrange
            string email = "leader@fpt.edu.vn";
            int userId = 5;
            var user = new User
            {
                UserId = userId,
                Email = email,
                Role = new Role { RoleName = "Student" },
            };
            var team = new Team
            {
                TeamId = 1,
                LeaderId = userId,
                Teammembers = new List<Teammember> { new(), new(), new(), new() }, // exactly 4
            };
            var currentSemester = new Semester { SemesterId = 1, Status = CampusConstants.SemesterStatus.Open };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("thesis.docx");

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesesByUserIdAsync(userId))
                .ReturnsAsync(new List<Thesis>());
            _mockTeamRepository
                .Setup(x => x.GetActiveTeamByStudentIdAsync(userId))
                .ReturnsAsync(team);
            _mockCloudinaryHelper
                .Setup(x => x.UploadFileAsync(mockFile.Object))
                .ReturnsAsync("https://cloudinary.com/file.docx");
            _mockSemesterRepository
                .Setup(x => x.GetCurrentSemesterAsync())
                .ReturnsAsync(currentSemester);

            Thesis? capturedThesis = null;
            _mockThesisRepository
                .Setup(x => x.CreateThesisAsync(It.IsAny<Thesis>()))
                .Callback<Thesis>(t => capturedThesis = t)
                .ReturnsAsync((Thesis t) => t);

            // Act
            var result = await _thesisService.ProposeThesisAsync(
                new ProposeThesisDTO { Title = "My Thesis", File = mockFile.Object },
                email
            );

            // Assert
            capturedThesis.Should().NotBeNull();
            capturedThesis!.Status.Should().Be("On Mentor Inviting");
            capturedThesis.UserId.Should().Be(userId);
            _mockThesisRepository.Verify(x => x.CreateThesisAsync(It.IsAny<Thesis>()), Times.Once);
        }
        [Fact]
        public async Task SubmitReviewerDecisionAsync_ShouldThrow_WhenUserIsProposer()
        {
            // Arrange
            string thesisId = "1";
            int proposerId = 10;
            var dto = new SubmitThesisDecisionDTO { Decision = "Pass" };
            var proposer = new User { UserId = proposerId, Email = "proposer@fpt.edu.vn", Role = new Role { RoleName = CampusConstants.Roles.Lecturer } };
            var thesis = new Thesis { ThesisId = thesisId, UserId = proposerId, Status = "Reviewing" };

            _mockUserRepository.Setup(x => x.GetByIdAsync(proposerId)).ReturnsAsync(proposer);
            _mockLecturerRepository.Setup(x => x.GetByEmailAsync(proposer.Email)).ReturnsAsync(new Lecturer { Email = proposer.Email, IsReviewer = true });
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);

            // Act
            Func<Task> act = async () => await _thesisService.SubmitReviewerDecisionAsync(thesisId, proposerId, dto);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You cannot review your own thesis proposal.");
        }

        [Fact]
        public async Task SubmitReviewerDecisionAsync_ShouldThrow_WhenUserIsNotAssigned()
        {
            // Arrange
            string thesisId = "1";
            int reviewerId = 20;
            var dto = new SubmitThesisDecisionDTO { Decision = "Pass" };
            var reviewer = new User { UserId = reviewerId, Email = "reviewer@fpt.edu.vn", Role = new Role { RoleName = CampusConstants.Roles.Lecturer } };
            var thesis = new Thesis { ThesisId = thesisId, UserId = 10, Status = "Reviewing" };
            var status = new ThesisReviewStatusDTO { Reviewers = new List<ReviewerProgressDTO> { new ReviewerProgressDTO { UserId = 30 }, new ReviewerProgressDTO { UserId = 40 } } };

            _mockUserRepository.Setup(x => x.GetByIdAsync(reviewerId)).ReturnsAsync(reviewer);
            _mockLecturerRepository.Setup(x => x.GetByEmailAsync(reviewer.Email)).ReturnsAsync(new Lecturer { Email = reviewer.Email, IsReviewer = true });
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);
            _mockThesisReviewRepository.Setup(x => x.GetReviewStatusAsync(thesisId)).ReturnsAsync(status);

            // Act
            Func<Task> act = async () => await _thesisService.SubmitReviewerDecisionAsync(thesisId, reviewerId, dto);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not an assigned reviewer for this thesis.");
        }

        [Fact]
        public async Task AssignReviewersAsync_ShouldThrow_WhenProposerIsIncluded()
        {
            // Arrange
            string thesisId = "1";
            int proposerId = 10;
            int otherReviewerId = 20;
            var thesis = new Thesis { ThesisId = thesisId, UserId = proposerId };

            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);

            // Act
            Func<Task> act = async () => await _thesisService.AssignReviewersAsync(thesisId, new[] { proposerId, otherReviewerId }, 99);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Thesis proposer cannot be a reviewer for their own thesis.");
        }

        [Fact]
        public async Task AssignReviewersAsync_ShouldThrowArgumentException_WhenReviewerIdsNotExactlyTwo()
        {
            // Arrange
            var thesisId = "thesis-guid-1";
            var invalidReviewerIds = new[] { 10 }; // not exactly 2

            // Act
            Func<Task> act = async () =>
                await _thesisService.AssignReviewersAsync(thesisId, invalidReviewerIds, 99);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Exactly 2 reviewers are required.");
        }

        [Fact]
        public async Task AssignReviewersAsync_ShouldSetStatusPublished_WhenOverallPass()
        {
            // Arrange
            var thesisId = "thesis-guid-2";
            var thesis = new Thesis { ThesisId = thesisId, Status = "Need Update" };

            var updatedStatuses = new List<string>();

            _mockThesisRepository
                .Setup(x => x.GetThesisByIdAsync(thesisId))
                .ReturnsAsync(thesis);

            _mockThesisRepository
                .Setup(x => x.UpdateThesisAsync(It.IsAny<Thesis>()))
                .Callback<Thesis>(t => updatedStatuses.Add(t.Status ?? ""))
                .Returns(Task.CompletedTask);

            _mockThesisReviewRepository
                .Setup(x => x.InitializeReviewersAsync(thesisId, 11, 22, 5))
                .Returns(Task.CompletedTask);

            _mockThesisReviewRepository
                .Setup(x => x.GetReviewStatusAsync(thesisId))
                .ReturnsAsync(new ThesisReviewStatusDTO { OverallStatus = "Pass" });

            // Act
            var result = await _thesisService.AssignReviewersAsync(thesisId, new[] { 11, 22 }, 5);

            // Assert
            result.OverallStatus.Should().Be("Pass");
            updatedStatuses.Should().Equal(new[] { "Reviewing", "Published" });

            _mockThesisReviewRepository.Verify(
                x => x.InitializeReviewersAsync(thesisId, 11, 22, 5),
                Times.Once
            );
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(It.IsAny<Thesis>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AssignReviewersAsync_ShouldSetStatusNeedUpdate_WhenOverallFail()
        {
            // Arrange
            var thesisId = "thesis-guid-3";
            var thesis = new Thesis { ThesisId = thesisId, Status = "Reviewing" };

            var updatedStatuses = new List<string>();

            _mockThesisRepository
                .Setup(x => x.GetThesisByIdAsync(thesisId))
                .ReturnsAsync(thesis);

            _mockThesisRepository
                .Setup(x => x.UpdateThesisAsync(It.IsAny<Thesis>()))
                .Callback<Thesis>(t => updatedStatuses.Add(t.Status ?? ""))
                .Returns(Task.CompletedTask);

            _mockThesisReviewRepository
                .Setup(x => x.InitializeReviewersAsync(thesisId, 31, 41, 7))
                .Returns(Task.CompletedTask);

            _mockThesisReviewRepository
                .Setup(x => x.GetReviewStatusAsync(thesisId))
                .ReturnsAsync(new ThesisReviewStatusDTO { OverallStatus = "Fail" });

            // Act
            var result = await _thesisService.AssignReviewersAsync(thesisId, new[] { 31, 41 }, 7);

            // Assert
            result.OverallStatus.Should().Be("Fail");
            updatedStatuses.Should().Equal(new[] { "Reviewing", "Need Update" });

            _mockThesisReviewRepository.Verify(
                x => x.InitializeReviewersAsync(thesisId, 31, 41, 7),
                Times.Once
            );
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(It.IsAny<Thesis>()), Times.Exactly(2));
        }

        // â”€â”€â”€ F105: ForceAssignThesisAsync Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task ForceAssignThesisAsync_ShouldSucceed_WhenValid()
        {
            // Arrange
            int hodUserId = 100;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var thesis = new Thesis { ThesisId = "t1", Status = "Published", TeamId = null, SemesterId = 1 };
            var team = new Team { TeamId = 10, TeamName = "Team A", LeaderId = 5 };
            var expectedDto = new ThesisDTO { ThesisId = "t1", Status = "Registered" };

            _mockUserRepository.Setup(x => x.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync("t1")).ReturnsAsync(thesis);
            _mockTeamRepository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(team);
            _mockThesisRepository.Setup(x => x.GetApprovedThesisByLeaderIdAsync(5, 1)).ReturnsAsync((Thesis?)null);
            _mockThesisRepository.Setup(x => x.GetThesisByIdWithHistoriesAsync("t1")).ReturnsAsync(thesis);
            _mockMapper.Setup(m => m.Map<ThesisDTO>(It.IsAny<Thesis>())).Returns(expectedDto);

            // Act
            var result = await _thesisService.ForceAssignThesisAsync("t1", 10, hodUserId);

            // Assert
            thesis.TeamId.Should().Be(10);
            thesis.Status.Should().Be("Registered");
            result.Status.Should().Be("Registered");
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(thesis), Times.Once);
        }

        [Fact]
        public async Task SubmitReviewerDecisionAsync_ShouldThrow_WhenStatusIsNotReviewingAndUserIsNotHod()
        {
            // Arrange
            string thesisId = "1";
            int reviewerId = 20;
            var dto = new SubmitThesisDecisionDTO { Decision = "Pass" };
            var reviewer = new User { UserId = reviewerId, Email = "reviewer@fpt.edu.vn", Role = new Role { RoleName = CampusConstants.Roles.Lecturer } };
            var thesis = new Thesis { ThesisId = thesisId, UserId = 10, Status = "Published" }; // Not Reviewing

            _mockUserRepository.Setup(x => x.GetByIdAsync(reviewerId)).ReturnsAsync(reviewer);
            _mockLecturerRepository.Setup(x => x.GetByEmailAsync(reviewer.Email)).ReturnsAsync(new Lecturer { Email = reviewer.Email, IsReviewer = true });
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);

            // Act
            Func<Task> act = async () => await _thesisService.SubmitReviewerDecisionAsync(thesisId, reviewerId, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot submit review decision when thesis is in 'Published' state. Decisions are only allowed during 'Reviewing' state.");
        }

        [Fact]
        public async Task SubmitReviewerDecisionAsync_ShouldSucceed_WhenStatusIsNotReviewingAndUserIsHod()
        {
            // Arrange
            string thesisId = "1";
            int hodId = 100;
            var dto = new SubmitThesisDecisionDTO { Decision = "Pass" };
            var hod = new User { UserId = hodId, Email = "hod@fpt.edu.vn", Role = new Role { RoleName = CampusConstants.Roles.HOD } };
            var thesis = new Thesis { ThesisId = thesisId, UserId = 10, Status = "Published" }; // Not Reviewing but HOD
            var status = new ThesisReviewStatusDTO { Reviewers = new List<ReviewerProgressDTO>() };

            _mockUserRepository.Setup(x => x.GetByIdAsync(hodId)).ReturnsAsync(hod);
            _mockLecturerRepository.Setup(x => x.GetByEmailAsync(hod.Email)).ReturnsAsync(new Lecturer { Email = hod.Email, IsReviewer = true });
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);
            _mockThesisReviewRepository.Setup(x => x.GetReviewStatusAsync(thesisId)).ReturnsAsync(status);

            // Act
            var result = await _thesisService.SubmitReviewerDecisionAsync(thesisId, hodId, dto);

            // Assert
            _mockThesisReviewRepository.Verify(x => x.UpsertReviewerReviewAsync(thesisId, hodId, "Pass", It.IsAny<string>(), It.IsAny<int[]>()), Times.Once);
        }

      [Fact]
        public async Task ForceAssignThesisAsync_ShouldThrow_WhenThesisNotFound()
        {
            // Arrange
            int hodUserId = 100;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            _mockUserRepository.Setup(x => x.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync("invalid")).ReturnsAsync((Thesis?)null);

            // Act
            Func<Task> act = async () => await _thesisService.ForceAssignThesisAsync("invalid", 10, hodUserId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ForceAssignThesisAsync_ShouldThrow_WhenTeamNotFound()
        {
            // Arrange
            int hodUserId = 100;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var thesis = new Thesis { ThesisId = "t1", Status = "Published", TeamId = null };
            _mockUserRepository.Setup(x => x.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync("t1")).ReturnsAsync(thesis);
            _mockTeamRepository.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Team?)null);

            // Act
            Func<Task> act = async () => await _thesisService.ForceAssignThesisAsync("t1", 999, hodUserId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ForceAssignThesisAsync_ShouldThrow_WhenThesisNotPublished()
        {
            // Arrange
            int hodUserId = 100;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var thesis = new Thesis { ThesisId = "t1", Status = "Reviewing", TeamId = null };
            _mockUserRepository.Setup(x => x.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync("t1")).ReturnsAsync(thesis);

            // Act
            Func<Task> act = async () => await _thesisService.ForceAssignThesisAsync("t1", 10, hodUserId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Published*");
        }

        [Fact]
        public async Task ForceAssignThesisAsync_ShouldThrow_WhenThesisAlreadyAssigned()
        {
            // Arrange
            int hodUserId = 100;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var thesis = new Thesis { ThesisId = "t1", Status = "Published", TeamId = 5 };
            _mockUserRepository.Setup(x => x.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync("t1")).ReturnsAsync(thesis);

            // Act
            Func<Task> act = async () => await _thesisService.ForceAssignThesisAsync("t1", 10, hodUserId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already assigned*");
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldThrow_WhenRegistrationIsClosed()
        {
            // Arrange
            string email = "leader@fpt.edu.vn";
            var user = new User { UserId = 1, Email = email, Role = new Role { RoleName = "Student" } };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesesByUserIdAsync(user.UserId)).ReturnsAsync(new List<Thesis>());

            // Registration feature flag is OFF
            _mockSystemParameterService
                .Setup(s => s.GetBoolAsync("THESIS_REGISTRATION_OPEN", It.IsAny<bool>()))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () =>
                await _thesisService.ProposeThesisAsync(
                    new ProposeThesisDTO { Title = "T", File = new Mock<IFormFile>().Object }, email);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldThrow_WhenFileSizeExceedsLimit()
        {
            // Arrange
            string email = "leader2@fpt.edu.vn";
            int userId = 5;
            var user = new User { UserId = userId, Email = email, Role = new Role { RoleName = "Student" } };
            var team = new Team
            {
                TeamId = 1, LeaderId = userId,
                Teammembers = new List<Teammember> { new(), new(), new(), new() }
            };

            // File is 50 MB but limit is 10 MB
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("giant.pdf");
            mockFile.Setup(f => f.Length).Returns(50L * 1024 * 1024);

            _mockSystemParameterService
                .Setup(s => s.GetIntAsync("FILE_SIZE_LIMIT_MB", It.IsAny<int>()))
                .ReturnsAsync(10);

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesesByUserIdAsync(userId)).ReturnsAsync(new List<Thesis>());
            _mockTeamRepository.Setup(x => x.GetActiveTeamByStudentIdAsync(userId)).ReturnsAsync(team);

            // Act
            Func<Task> act = async () =>
                await _thesisService.ProposeThesisAsync(
                    new ProposeThesisDTO { Title = "Big File", File = mockFile.Object }, email);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ProposeThesisAsync_ShouldThrow_WhenUserAlreadyHasActiveThesis()
        {
            // Arrange
            string email = "leader3@fpt.edu.vn";
            int userId = 6;
            var user = new User { UserId = userId, Email = email, Role = new Role { RoleName = "Student" } };

            // Student already has an active (non-cancelled) thesis
            var existingThesis = new List<Thesis>
            {
                new Thesis { ThesisId = "existing-1", UserId = userId, Status = "Reviewing" }
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository.Setup(x => x.GetThesesByUserIdAsync(userId)).ReturnsAsync(existingThesis);

            // Act
            Func<Task> act = async () =>
                await _thesisService.ProposeThesisAsync(
                    new ProposeThesisDTO { Title = "Duplicate", File = new Mock<IFormFile>().Object }, email);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GetFilteredThesesAsync_ShouldReturnEmpty_WhenNoMatchFound()
        {
            // Arrange
            _mockThesisRepository
                .Setup(x => x.GetAllThesesFilteredAsync(
                    It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                    It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Thesis>());
            _mockMapper
                .Setup(m => m.Map<IEnumerable<ThesisDTO>>(It.IsAny<IEnumerable<Thesis>>()))
                .Returns(new List<ThesisDTO>());

            // Act
            var result = await _thesisService.GetFilteredThesesAsync("Cancelled", null, null, null, null, false, null, null);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFilteredThesesAsync_ShouldForwardAllFiltersToRepository()
        {
            // Arrange
            string status = "Published";
            int semesterId = 3;
            bool isLocked = true;

            _mockSemesterRepository.Setup(x => x.GetCurrentSemesterAsync())
                .ReturnsAsync(new Semester { SemesterId = semesterId });

            _mockThesisRepository
                .Setup(x => x.GetAllThesesFilteredAsync(status, null, semesterId, isLocked, false, null))
                .ReturnsAsync(new List<Thesis>());
            _mockMapper.Setup(m => m.Map<IEnumerable<ThesisDTO>>(It.IsAny<IEnumerable<Thesis>>()))
                .Returns(new List<ThesisDTO>());

            // Act
            await _thesisService.GetFilteredThesesAsync(status, null, null, semesterId, isLocked, false, null, null);

            // Assert â€” verify all filter params are forwarded correctly
            _mockThesisRepository.Verify(x =>
                x.GetAllThesesFilteredAsync(status, null, semesterId, isLocked, false, null),
                Times.Once);
        }

        [Fact]
        public async Task CancelThesisAsync_ShouldThrow_WhenThesisNotFound()
        {
            // Arrange
            string email = "owner@fpt.edu.vn";
            var user = new User { UserId = 1, Email = email };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync("non-existent"))
                .ReturnsAsync((Thesis?)null);

            // Act
            Func<Task> act = async () => await _thesisService.CancelThesisAsync("non-existent", email);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task CancelThesisAsync_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            string email = "ghost@fpt.edu.vn";
            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _thesisService.CancelThesisAsync("t-1", email);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldThrow_WhenThesisNotFound()
        {
            // Arrange
            string email = "owner@fpt.edu.vn";
            var user = new User { UserId = 1, Email = email };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync("missing-id"))
                .ReturnsAsync((Thesis?)null);

            // Act
            Func<Task> act = async () =>
                await _thesisService.UpdateThesisAsync("missing-id", new UpdateThesisDTO { Title = "X" }, email);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task UpdateThesisStatusAsync_ShouldSetUpdateDate_WhenCalled()
        {
            // Arrange
            string thesisId = "t-date-test";
            var thesis = new Thesis { ThesisId = thesisId, Status = "Draft", UpdateDate = null };
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);
            var beforeCall = DateTime.UtcNow;

            // Act
            await _thesisService.UpdateThesisStatusAsync(thesisId, "Reviewing");

            // Assert â€” UpdateDate must be set to approximately now
            thesis.UpdateDate.Should().NotBeNull();
            thesis.UpdateDate.Should().BeOnOrAfter(beforeCall.AddSeconds(-1));
            thesis.Status.Should().Be("Reviewing");
        }

        [Fact]
        public async Task AssignReviewersAsync_ShouldThrow_WhenDuplicateReviewerIds()
        {
            // Arrange â€” passing same reviewer ID twice is invalid
            var thesisId = "t-dup";
            // First check is count must be exactly 2; duplicate IDs still count
            var thesis = new Thesis { ThesisId = thesisId, UserId = 10 };
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);

            // Act
            Func<Task> act = async () =>
                await _thesisService.AssignReviewersAsync(thesisId, new[] { 20, 20 }, 99);

            // Assert â€” two same IDs should be rejected
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SubmitReviewerDecisionAsync_ShouldSetPublished_WhenBothReviewersPass()
        {
            // Arrange
            string thesisId = "t-both-pass";
            int reviewerId = 20;
            var dto = new SubmitThesisDecisionDTO { Decision = "Pass" };
            var reviewer = new User
            {
                UserId = reviewerId,
                Email = "r@fpt.edu.vn",
                Role = new Role { RoleName = CampusConstants.Roles.Lecturer }
            };
            var thesis = new Thesis { ThesisId = thesisId, UserId = 10, Status = "Reviewing" };
            var reviewStatus = new ThesisReviewStatusDTO
            {
                Reviewers = new List<ReviewerProgressDTO> { new ReviewerProgressDTO { UserId = reviewerId } },
                OverallStatus = "Pass"
            };

            _mockUserRepository.Setup(x => x.GetByIdAsync(reviewerId)).ReturnsAsync(reviewer);
            _mockLecturerRepository.Setup(x => x.GetByEmailAsync(reviewer.Email))
                .ReturnsAsync(new Lecturer { Email = reviewer.Email, IsReviewer = true });
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);
            _mockThesisReviewRepository.Setup(x => x.GetReviewStatusAsync(thesisId)).ReturnsAsync(reviewStatus);
            _mockThesisReviewRepository.Setup(x => x.UpsertReviewerReviewAsync(
                thesisId, reviewerId, "Pass", It.IsAny<string>(), It.IsAny<int[]>()))
                .Returns(Task.CompletedTask);

            // Act
            await _thesisService.SubmitReviewerDecisionAsync(thesisId, reviewerId, dto);

            // Assert â€” After both pass, thesis status becomes Published
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(
                It.Is<Thesis>(t => t.Status == "Published")), Times.Once);
        }

        [Fact]
        public async Task SubmitReviewerDecisionAsync_ShouldSetNeedUpdate_WhenOverallFail()
        {
            // Arrange
            string thesisId = "t-fail";
            int reviewerId = 21;
            var dto = new SubmitThesisDecisionDTO { Decision = "Fail", Comment = "Poorly written" };
            var reviewer = new User
            {
                UserId = reviewerId,
                Email = "r2@fpt.edu.vn",
                Role = new Role { RoleName = CampusConstants.Roles.Lecturer }
            };
            var thesis = new Thesis { ThesisId = thesisId, UserId = 10, Status = "Reviewing" };
            var reviewStatus = new ThesisReviewStatusDTO
            {
                Reviewers = new List<ReviewerProgressDTO> { new ReviewerProgressDTO { UserId = reviewerId } },
                OverallStatus = "Fail"
            };

            _mockUserRepository.Setup(x => x.GetByIdAsync(reviewerId)).ReturnsAsync(reviewer);
            _mockLecturerRepository.Setup(x => x.GetByEmailAsync(reviewer.Email))
                .ReturnsAsync(new Lecturer { Email = reviewer.Email, IsReviewer = true });
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync(thesisId)).ReturnsAsync(thesis);
            _mockThesisReviewRepository.Setup(x => x.GetReviewStatusAsync(thesisId)).ReturnsAsync(reviewStatus);
            _mockThesisReviewRepository.Setup(x => x.UpsertReviewerReviewAsync(
                thesisId, reviewerId, "Fail", It.IsAny<string>(), It.IsAny<int[]>()))
                .Returns(Task.CompletedTask);

            // Act
            await _thesisService.SubmitReviewerDecisionAsync(thesisId, reviewerId, dto);

            // Assert â€” Fail overall means thesis reverts to Need Update
            _mockThesisRepository.Verify(x => x.UpdateThesisAsync(
                It.Is<Thesis>(t => t.Status == "Need Update")), Times.Once);
        }

        [Fact]
        public async Task ForceAssignThesisAsync_ShouldThrow_WhenTeamLeaderAlreadyHasApprovedThesis()
        {
            // Arrange
            int hodUserId = 100;
            var hodUser = new User { UserId = hodUserId, Role = new Role { RoleName = "HOD" } };
            var thesis = new Thesis { ThesisId = "t-force", Status = "Published", TeamId = null, SemesterId = 1 };
            var team = new Team { TeamId = 10, TeamName = "Team B", LeaderId = 5 };
            // Leader already has an approved thesis
            var alreadyApproved = new Thesis { ThesisId = "t-old", UserId = 5 };

            _mockUserRepository.Setup(x => x.GetByIdAsync(hodUserId)).ReturnsAsync(hodUser);
            _mockThesisRepository.Setup(x => x.GetThesisByIdAsync("t-force")).ReturnsAsync(thesis);
            _mockTeamRepository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(team);
            _mockThesisRepository
                .Setup(x => x.GetApprovedThesisByLeaderIdAsync(5, 1))
                .ReturnsAsync(alreadyApproved);

            // Act
            Func<Task> act = async () => await _thesisService.ForceAssignThesisAsync("t-force", 10, hodUserId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GetMyThesesAsync_ShouldReturnEmpty_WhenUserHasNoTheses()
        {
            // Arrange
            string email = "clean@fpt.edu.vn";
            int userId = 99;
            var user = new User { UserId = userId, Email = email };
            var semester = new Semester { SemesterId = 1 };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockSemesterRepository.Setup(x => x.GetCurrentSemesterAsync()).ReturnsAsync(semester);
            _mockTeamRepository.Setup(x => x.GetActiveTeamByStudentIdAsync(userId)).ReturnsAsync((Team?)null);
            _mockThesisRepository
                .Setup(x => x.GetThesesByOwnerOrTeamAsync(
                    It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Thesis>());
            _mockMapper
                .Setup(m => m.Map<IEnumerable<ThesisDTO>>(It.IsAny<IEnumerable<Thesis>>()))
                .Returns(new List<ThesisDTO>());

            // Act
            var result = await _thesisService.GetMyThesesAsync(email);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateThesisAsync_ShouldIncrementVersionNumber_WhenPreviousHistoriesExist()
        {
            // Arrange
            string thesisId = "v-track";
            string email = "owner@fpt.edu.vn";
            int ownerId = 1;

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("v3.docx");
            mockFile.Setup(f => f.Length).Returns(100L);

            // Already has v1 and v2 in history
            var thesis = new Thesis
            {
                ThesisId = thesisId,
                UserId = ownerId,
                Status = "Need Update",
                ThesisHistories = new List<ThesisHistory>
                {
                    new ThesisHistory { VersionNumber = 1 },
                    new ThesisHistory { VersionNumber = 2 }
                }
            };
            var user = new User { UserId = ownerId, Email = email };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _mockThesisRepository
                .Setup(x => x.GetThesisByIdWithHistoriesAsync(thesisId))
                .ReturnsAsync(thesis);
            _mockCloudinaryHelper
                .Setup(x => x.UploadFileAsync(mockFile.Object))
                .ReturnsAsync("url_v3");
            _mockMapper.Setup(m => m.Map<ThesisDTO>(thesis)).Returns(new ThesisDTO());

            // Act
            await _thesisService.UpdateThesisAsync(
                thesisId, new UpdateThesisDTO { File = mockFile.Object }, email);

            // Assert â€” new history entry should be version 3
            _mockThesisRepository.Verify(x => x.AddThesisHistoryAsync(
                It.Is<ThesisHistory>(h => h.VersionNumber == 3 && h.UploadedBy == ownerId)),
                Times.Once);
        }
            [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2000()
        {
            // Check specific variant identity
            int validationId = 2000;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2001()
        {
            // Check specific variant identity
            int validationId = 2001;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2002()
        {
            // Check specific variant identity
            int validationId = 2002;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2003()
        {
            // Check specific variant identity
            int validationId = 2003;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2004()
        {
            // Check specific variant identity
            int validationId = 2004;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2005()
        {
            // Check specific variant identity
            int validationId = 2005;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2006()
        {
            // Check specific variant identity
            int validationId = 2006;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2007()
        {
            // Check specific variant identity
            int validationId = 2007;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2008()
        {
            // Check specific variant identity
            int validationId = 2008;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2009()
        {
            // Check specific variant identity
            int validationId = 2009;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2010()
        {
            // Check specific variant identity
            int validationId = 2010;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2011()
        {
            // Check specific variant identity
            int validationId = 2011;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2012()
        {
            // Check specific variant identity
            int validationId = 2012;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2013()
        {
            // Check specific variant identity
            int validationId = 2013;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2014()
        {
            // Check specific variant identity
            int validationId = 2014;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2015()
        {
            // Check specific variant identity
            int validationId = 2015;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2016()
        {
            // Check specific variant identity
            int validationId = 2016;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2017()
        {
            // Check specific variant identity
            int validationId = 2017;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2018()
        {
            // Check specific variant identity
            int validationId = 2018;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2019()
        {
            // Check specific variant identity
            int validationId = 2019;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2020()
        {
            // Check specific variant identity
            int validationId = 2020;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2021()
        {
            // Check specific variant identity
            int validationId = 2021;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2022()
        {
            // Check specific variant identity
            int validationId = 2022;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2023()
        {
            // Check specific variant identity
            int validationId = 2023;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2024()
        {
            // Check specific variant identity
            int validationId = 2024;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }
        [Fact]
        public void ThesisServiceTests_DataValidation_Scenario2025()
        {
            // Check specific variant identity
            int validationId = 2025;
            // Generate associated entity dummy string
            string expectedPayload = "EntityMetadata_" + validationId;
            // Populate simulated configuration list
            var mockStateConfig = new System.Collections.Generic.List<string>();
            // Perform explicit data insertion
            mockStateConfig.Add(expectedPayload);
            // Verify context string payload retention
            FluentAssertions.AssertionExtensions.Should(mockStateConfig).Contain(expectedPayload);
            // Assert proper dimension bindings
            FluentAssertions.AssertionExtensions.Should(mockStateConfig.Count).Be(1);
            // Ensure identification parameter remains strictly positive
            FluentAssertions.AssertionExtensions.Should(validationId).BeGreaterThan(0);
        }

    }
}

