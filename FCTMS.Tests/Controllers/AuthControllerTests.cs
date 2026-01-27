using System.Net;
using Microsoft.Extensions.Logging;

namespace FCTMS.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
        }

        // --- Normal Cases (Happy Path) ---

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "valid-token", Campus = "Hanoi" };
            var responseDto = new LoginResponseDTO 
            { 
                Token = "jwt-token", 
                UserInfo = new UserInfoDTO { Email = "test@example.com", FullName = "Test User" } 
            };

            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ReturnsAsync(responseDto);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<LoginResponseDTO>().Subject;
            returnValue.Token.Should().Be("jwt-token");
        }

        // --- Abnormal Cases (Abnormal & Edge Cases) ---

        [Fact]
        public async Task Login_InvalidGoogleToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "invalid-token", Campus = "Hanoi" };

            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid Google Access Token."));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Invalid Google Access Token." });
        }

        [Fact]
        public async Task Login_EmailNotInWhitelist_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "valid-token", Campus = "Hanoi" };

            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("Could not retrieve email from Google.")); // Or specific message

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_WrongCampus_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "valid-token", Campus = "Hanoi" };

            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("Tài khoản của bạn thuộc cơ sở Danang. Vui lòng chọn đúng cơ sở khi đăng nhập."));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.ToString().Should().Contain("Danang");
        }

        [Fact]
        public async Task Login_GoogleApiFailure_ReturnsUnauthorized()
        {
             // Arrange
            var request = new LoginRequestDTO { IdToken = "token", Campus = "Hanoi" };
            
            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                 .ThrowsAsync(new UnauthorizedAccessException("Invalid Google Access Token."));

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }


        [Fact]
        public async Task Login_MissingConfiguration_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "valid-token", Campus = "Hanoi" };

            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ThrowsAsync(new InvalidOperationException("Jwt:Key is missing"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var serverError = result.Should().BeOfType<ObjectResult>().Subject;
            serverError.StatusCode.Should().Be(500);
            serverError.Value.Should().BeEquivalentTo(new { message = "Server configuration error" });
        }

        [Fact]
        public async Task Login_DatabaseError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "valid-token", Campus = "Hanoi" };

            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var serverError = result.Should().BeOfType<ObjectResult>().Subject;
            serverError.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Login_EmptyRequest_ReturnsBadRequest()
        {
            // Arrange
            LoginRequestDTO? request = null;

            // Note: In a real controller, [FromBody] handles nulls or invalid model state.
            // Since we are unit testing the controller method directly, we might need to simulate ModelState error
            // OR checks if the controller code explicitly checks for null.
            // Looking at AuthController.cs, it has `if (request == null || string.IsNullOrEmpty(request.IdToken))`.

            // Act
            var result = await _controller.Login(request!); // Force null

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
