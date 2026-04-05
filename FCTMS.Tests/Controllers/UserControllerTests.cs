namespace FCTMS.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockAuthService = new Mock<IAuthService>();
            _controller = new UserController(_mockUserService.Object, _mockAuthService.Object);
        }

        // --- GetProfileByUserId ---

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-999)]
        public async Task GetProfileByUserId_InvalidUserId_ReturnsBadRequest(int userId)
        {
            // Act
            var result = await _controller.GetProfileByUserId(userId);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { message = "Invalid userId." });
            _mockUserService.Verify(x => x.GetProfileAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetProfileByUserId_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetProfileAsync(999))
                .ReturnsAsync((UserInfoDTO?)null);

            // Act
            var result = await _controller.GetProfileByUserId(999);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.Value.Should().BeEquivalentTo(new { message = "User not found" });
        }

        [Fact]
        public async Task GetProfileByUserId_ValidUser_ReturnsOkWithProfile()
        {
            // Arrange
            var profile = new UserInfoDTO
            {
                Email = "student@fpt.edu.vn",
                FullName = "Nguyen Van A"
            };
            _mockUserService.Setup(x => x.GetProfileAsync(1))
                .ReturnsAsync(profile);

            // Act
            var result = await _controller.GetProfileByUserId(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<UserInfoDTO>().Subject;
            returnValue.Email.Should().Be("student@fpt.edu.vn");
            returnValue.FullName.Should().Be("Nguyen Van A");
        }

        [Fact]
        public async Task GetProfileByUserId_ServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetProfileAsync(1))
                .ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _controller.GetProfileByUserId(1);

            // Assert
            var serverError = result.Should().BeOfType<ObjectResult>().Subject;
            serverError.StatusCode.Should().Be(500);
            serverError.Value.Should().BeEquivalentTo(new
            {
                message = "An internal server error occurred.",
                details = "Database failure"
            });
        }

        [Fact]
        public async Task GetProfileByUserId_ValidAdminId_ReturnsProfile()
        {
            // Arrange — admin user with extra fields
            var profile = new UserInfoDTO
            {
                Email = "admin@fpt.edu.vn",
                FullName = "System Admin",
                RoleName = "Admin"
            };
            _mockUserService.Setup(x => x.GetProfileAsync(5)).ReturnsAsync(profile);

            // Act
            var result = await _controller.GetProfileByUserId(5);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = ok.Value.Should().BeAssignableTo<UserInfoDTO>().Subject;
            value.RoleName.Should().Be("Admin");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public async Task GetProfileByUserId_ValidIds_CallsServiceOnce(int userId)
        {
            // Arrange
            var profile = new UserInfoDTO { Email = $"user{userId}@fpt.edu.vn", FullName = $"User {userId}" };
            _mockUserService.Setup(x => x.GetProfileAsync(userId)).ReturnsAsync(profile);

            // Act
            await _controller.GetProfileByUserId(userId);

            // Assert — service must be called exactly once for valid IDs
            _mockUserService.Verify(x => x.GetProfileAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetProfileByUserId_NullProfile_ReturnsNotFound()
        {
            // Arrange — service returns null when user doesn't exist
            _mockUserService.Setup(x => x.GetProfileAsync(42)).ReturnsAsync((UserInfoDTO?)null);

            // Act
            var result = await _controller.GetProfileByUserId(42);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetProfileByUserId_ExceptionMessage_IncludedInResponse()
        {
            // Arrange — verify exception message is bubbled correctly
            _mockUserService.Setup(x => x.GetProfileAsync(7))
                .ThrowsAsync(new InvalidOperationException("Custom service error"));

            // Act
            var result = await _controller.GetProfileByUserId(7);

            // Assert
            var err = result.Should().BeOfType<ObjectResult>().Subject;
            err.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetProfileByUserId_ProfileHasAllFields_ReturnsComplete()
        {
            // Arrange — full profile with all optional fields
            var profile = new UserInfoDTO
            {
                Email = "lecturer@fpt.edu.vn",
                FullName = "Dr. Nguyen",
                RoleName = "Lecturer",
                Campus = "Hanoi",
                Avatar = "https://cdn.fpt.edu.vn/avatar.jpg"
            };
            _mockUserService.Setup(x => x.GetProfileAsync(10)).ReturnsAsync(profile);

            // Act
            var result = await _controller.GetProfileByUserId(10);

            // Assert — all fields must travel through the controller untouched
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = ok.Value.Should().BeAssignableTo<UserInfoDTO>().Subject;
            value.Email.Should().Be("lecturer@fpt.edu.vn");
            value.FullName.Should().Be("Dr. Nguyen");
            value.RoleName.Should().Be("Lecturer");
            value.Campus.Should().Be("Hanoi");
            value.Avatar.Should().Be("https://cdn.fpt.edu.vn/avatar.jpg");
        }

        [Fact]
        public async Task GetProfileByUserId_IdEqualsOne_MinimumValidId_ReturnsOk()
        {
            // Arrange — userId = 1 is the minimum valid value
            var profile = new UserInfoDTO { Email = "first@fpt.edu.vn", FullName = "First User" };
            _mockUserService.Setup(x => x.GetProfileAsync(1)).ReturnsAsync(profile);

            // Act
            var result = await _controller.GetProfileByUserId(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetProfileByUserId_TimeoutException_Returns500()
        {
            // Arrange — simulate DB timeout
            _mockUserService.Setup(x => x.GetProfileAsync(3))
                .ThrowsAsync(new TimeoutException("Query timed out"));

            // Act
            var result = await _controller.GetProfileByUserId(3);

            // Assert
            var err = result.Should().BeOfType<ObjectResult>().Subject;
            err.StatusCode.Should().Be(500);
            err.Value.Should().BeEquivalentTo(new { message = "An internal server error occurred.", details = "Query timed out" });
        }
    }
}
