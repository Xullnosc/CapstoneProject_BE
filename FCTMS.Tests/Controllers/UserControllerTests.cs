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
    }
}
