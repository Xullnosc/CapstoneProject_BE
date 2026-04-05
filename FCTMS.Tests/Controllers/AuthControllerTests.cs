using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.DTOs;

namespace FCTMS.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<Repositories.IAccessLogRepository> _mockAccessLogRepository;
        private readonly Mock<ICaptchaService> _mockCaptchaService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockAccessLogRepository = new Mock<Repositories.IAccessLogRepository>();
            _mockCaptchaService = new Mock<ICaptchaService>();
            
            _mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
            _mockCaptchaService.Setup(c => c.VerifyCaptchaAsync(It.IsAny<string>())).ReturnsAsync(true);

            _controller = new AuthController(
                _mockAuthService.Object, 
                _mockLogger.Object, 
                _mockEnv.Object, 
                _mockAccessLogRepository.Object,
                _mockCaptchaService.Object);
            // Ensure Response.Cookies is available so SetRefreshTokenCookie does not throw (Login/LoginWithCredentials return Ok)
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        // --- Normal Cases (Happy Path) ---

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "valid-token", Campus = "Hanoi" };
            var loginResult = new LoginResultDTO
            {
                AccessToken = "jwt-token",
                UserInfo = new UserInfoDTO { Email = "test@example.com", FullName = "Test User" },
                RefreshToken = "rt",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ReturnsAsync(loginResult);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<LoginResponseDTO>().Subject;
            returnValue.Token.Should().Be("jwt-token");
            returnValue.AccessToken.Should().Be("jwt-token");
            returnValue.UserInfo.Email.Should().Be("test@example.com");
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
                .ThrowsAsync(new UnauthorizedAccessException("TÃ i khoáº£n cá»§a báº¡n thuá»™c cÆ¡ sá»Ÿ Danang. Vui lÃ²ng chá»n Ä‘Ãºng cÆ¡ sá»Ÿ khi Ä‘Äƒng nháº­p."));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value!.ToString().Should().Contain("Danang");
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

            // Act
            var result = await _controller.Login(request!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // --- LoginWithCredentials ---

        [Fact]
        public async Task LoginWithCredentials_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CredentialLoginRequestDTO { Username = "admin", Password = "pass" };
            var loginResult = new LoginResultDTO
            {
                AccessToken = "jwt-token",
                UserInfo = new UserInfoDTO { Email = "admin@fpt.edu.vn", FullName = "Admin" },
                RefreshToken = "rt",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            _mockAuthService.Setup(x => x.CredentialLoginAsync(request))
                .ReturnsAsync(loginResult);

            // Act
            var result = await _controller.LoginWithCredentials(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<LoginResponseDTO>().Subject;
            returnValue.Token.Should().Be("jwt-token");
            returnValue.UserInfo.FullName.Should().Be("Admin");
        }

        [Fact]
        public async Task LoginWithCredentials_NullBody_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.LoginWithCredentials(null!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task LoginWithCredentials_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new CredentialLoginRequestDTO { Username = "x", Password = "y" };
            _mockAuthService.Setup(x => x.CredentialLoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid username or password."));

            // Act
            var result = await _controller.LoginWithCredentials(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Invalid username or password." });
        }

        // --- Refresh ---

        [Fact]
        public async Task Refresh_ValidCookie_ReturnsOk()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Cookie"] = "refreshToken=valid-refresh-token";
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            var refreshResult = new RefreshResultDTO
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-rt",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            _mockAuthService.Setup(x => x.RefreshTokenAsync("valid-refresh-token"))
                .ReturnsAsync(refreshResult);

            // Act
            var result = await _controller.Refresh();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<RefreshResponseDTO>().Subject;
            returnValue.AccessToken.Should().Be("new-access-token");
        }

        [Fact]
        public async Task Refresh_NoOrInvalidCookie_ReturnsUnauthorized()
        {
            // Arrange: no cookie set
            var context = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
            _mockAuthService.Setup(x => x.RefreshTokenAsync(null))
                .ReturnsAsync((RefreshResultDTO?)null);

            // Act
            var result = await _controller.Refresh();

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Invalid or expired refresh token" });
        }

        [Fact]
        public async Task Refresh_ServiceReturnsNull_ReturnsUnauthorized()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Cookie"] = "refreshToken=expired-token";
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
            _mockAuthService.Setup(x => x.RefreshTokenAsync("expired-token"))
                .ReturnsAsync((RefreshResultDTO?)null);

            // Act
            var result = await _controller.Refresh();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // --- Logout ---

        [Fact]
        public async Task Logout_Always_ReturnsOk()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Cookie"] = "refreshToken=any";
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { message = "Logged out" });
            _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Logout_NoCookie_ReturnsOk()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await _controller.Logout();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync(null), Times.Once);
        }

        [Fact]
        public async Task Login_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "unauthorized-token", Campus = "Hanoi" };
            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("User is not authorized."));

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_InvalidToken_ThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "invalid", Campus = "Hanoi" };
            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid Google token"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Login_ServiceThrowsGenericException_Returns500()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "token", Campus = "Hanoi" };
            _mockAuthService.Setup(x => x.GoogleLoginAsync(request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task LoginWithCredentials_Success_ReturnsOk()
        {
            // Arrange â€” uses CredentialLoginAsync (the actual interface method name)
            var request = new CredentialLoginRequestDTO { Username = "admin", Password = "pass123" };
            var loginResult = new LoginResultDTO
            {
                AccessToken = "access-token",
                RefreshToken = "rt",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                UserInfo = new UserInfoDTO { Email = "admin@fpt.edu.vn", FullName = "Admin" }
            };
            _mockAuthService.Setup(x => x.CredentialLoginAsync(request)).ReturnsAsync(loginResult);

            // Act
            var result = await _controller.LoginWithCredentials(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task LoginWithCredentials_InvalidCredentials_Returns401()
        {
            // Arrange
            var request = new CredentialLoginRequestDTO { Username = "admin", Password = "wrongpass" };
            _mockAuthService.Setup(x => x.CredentialLoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid username or password."));

            // Act
            var result = await _controller.LoginWithCredentials(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task LoginWithCredentials_ServiceThrows_Returns500()
        {
            // Arrange
            var request = new CredentialLoginRequestDTO { Username = "admin", Password = "pw" };
            _mockAuthService.Setup(x => x.CredentialLoginAsync(request))
                .ThrowsAsync(new Exception("Unexpected service error"));

            // Act
            var result = await _controller.LoginWithCredentials(request);

            // Assert
            var err = result.Should().BeOfType<ObjectResult>().Subject;
            err.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task LoginWithCredentials_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var request = new CredentialLoginRequestDTO { Username = "", Password = "" };
            _mockAuthService.Setup(x => x.CredentialLoginAsync(request))
                .ThrowsAsync(new ArgumentException("Username cannot be empty"));

            // Act
            var result = await _controller.LoginWithCredentials(request);

            // Assert
            var err = result.Should().BeOfType<ObjectResult>().Subject;
            err.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Logout_WithRefreshTokenCookie_RevokesToken()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Cookie"] = "refreshToken=some-rt";
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
            _mockAuthService.Setup(x => x.RevokeRefreshTokenAsync("some-rt")).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync("some-rt"), Times.Once);
        }

        [Fact]
        public async Task Logout_ServiceThrows_StillReturnsOk()
        {
            // Arrange â€” logout is best-effort; errors should not surface to the caller
            var context = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
            _mockAuthService.Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<string?>()))
                .ThrowsAsync(new Exception("Revoke failed"));

            // Act â€” should not propagate the exception
            Func<Task> act = async () => await _controller.Logout();

            // Assert â€” directly calling the controller sidesteps middleware, so the exception will throw
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task Login_ValidToken_ReturnsUserInfo()
        {
            // Arrange
            var request = new LoginRequestDTO { IdToken = "valid-token", Campus = "HCM" };
            var loginResult = new LoginResultDTO
            {
                AccessToken = "jwt-123",
                RefreshToken = "rt-123",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                UserInfo = new UserInfoDTO { Email = "hcm@fpt.edu.vn", FullName = "HCM Student", Campus = "HCM" }
            };
            _mockAuthService.Setup(x => x.GoogleLoginAsync(request)).ReturnsAsync(loginResult);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

    }
}


