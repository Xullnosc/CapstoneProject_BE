using Microsoft.Extensions.Logging;

namespace FCTMS.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IAdminService> _mockAdminService;
    private readonly Mock<IAccessLogService> _mockAccessLogService;
    private readonly Mock<ILogger<AdminController>> _mockLogger;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mockAdminService = new Mock<IAdminService>();
        _mockAccessLogService = new Mock<IAccessLogService>();
        _mockLogger = new Mock<ILogger<AdminController>>();
        _controller = new AdminController(_mockAdminService.Object, _mockAccessLogService.Object, _mockLogger.Object);
    }

    // --- GetHodAccounts (GET /api/Admin/hod) ---

    [Fact]
    public async Task GetHodAccounts_ReturnsOk_WithList()
    {
        // Arrange
        var list = new List<HodAccountDTO>
        {
            new HodAccountDTO { UserId = 1, FullName = "HOD One", Email = "hod1@test.com", Username = "hod1", HasCredential = true }
        };
        _mockAdminService.Setup(x => x.GetHodAccountsAsync(null)).ReturnsAsync(list);

        // Act
        var result = await _controller.GetHodAccounts(null);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task GetHodAccounts_WithSearch_CallsServiceWithSearch()
    {
        // Arrange
        var emptyList = new List<HodAccountDTO>();
        _mockAdminService.Setup(x => x.GetHodAccountsAsync("email")).ReturnsAsync(emptyList);

        // Act
        var result = await _controller.GetHodAccounts("email");

        // Assert
        _mockAdminService.Verify(x => x.GetHodAccountsAsync("email"), Times.Once);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(emptyList);
    }

    [Fact]
    public async Task GetHodAccounts_ServiceThrows_Returns500()
    {
        // Arrange
        _mockAdminService.Setup(x => x.GetHodAccountsAsync(It.IsAny<string?>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _controller.GetHodAccounts(null);

        // Assert
        var status = result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(500);
        status.Value.Should().NotBeNull();
        var value = status.Value;
        var msgProp = value?.GetType().GetProperty("message");
        msgProp.Should().NotBeNull();
        msgProp!.GetValue(value).Should().Be("DB error");
    }

    // --- CreateOrUpdateHod (POST /api/Admin/hod) ---

    [Fact]
    public async Task CreateOrUpdateHod_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateOrUpdateHodDTO
        {
            FullName = "HOD Name",
            Email = "hod@test.com",
            Username = "hoduser",
            Password = "secret"
        };
        _mockAdminService.Setup(x => x.CreateOrUpdateHodAsync(dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreateOrUpdateHod(dto);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
        var value = ok.Value;
        var msgProp = value?.GetType().GetProperty("message");
        msgProp.Should().NotBeNull();
        msgProp!.GetValue(value).Should().Be("HOD account created or updated successfully.");
    }

    [Fact]
    public async Task CreateOrUpdateHod_NullDto_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.CreateOrUpdateHod(null!);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
        var value = badRequest.Value;
        var msgProp = value?.GetType().GetProperty("message");
        msgProp.Should().NotBeNull();
        msgProp!.GetValue(value).Should().Be("Request body is null");
    }

    [Fact]
    public async Task CreateOrUpdateHod_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateOrUpdateHodDTO
        {
            FullName = "HOD",
            Email = "hod@test.com",
            Username = "hod",
            Password = "pwd"
        };
        _mockAdminService.Setup(x => x.CreateOrUpdateHodAsync(dto))
            .ThrowsAsync(new ArgumentException("Invalid email"));

        // Act
        var result = await _controller.CreateOrUpdateHod(dto);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
        var value = badRequest.Value;
        var msgProp = value?.GetType().GetProperty("message");
        msgProp.Should().NotBeNull();
        msgProp!.GetValue(value).Should().Be("Invalid email");
    }

    [Fact]
    public async Task CreateOrUpdateHod_InvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateOrUpdateHodDTO
        {
            FullName = "HOD",
            Email = "hod@test.com",
            Username = "hod",
            Password = "pwd"
        };
        _mockAdminService.Setup(x => x.CreateOrUpdateHodAsync(dto))
            .ThrowsAsync(new InvalidOperationException("Username already exists"));

        // Act
        var result = await _controller.CreateOrUpdateHod(dto);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
        var value = badRequest.Value;
        var msgProp = value?.GetType().GetProperty("message");
        msgProp.Should().NotBeNull();
        msgProp!.GetValue(value).Should().Be("Username already exists");
    }

    [Fact]
    public async Task CreateOrUpdateHod_OtherException_Returns500()
    {
        // Arrange
        var dto = new CreateOrUpdateHodDTO
        {
            FullName = "HOD",
            Email = "hod@test.com",
            Username = "hod",
            Password = "pwd"
        };
        _mockAdminService.Setup(x => x.CreateOrUpdateHodAsync(dto))
            .ThrowsAsync(new Exception("Unexpected failure"));

        // Act
        var result = await _controller.CreateOrUpdateHod(dto);

        // Assert
        var status = result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(500);
        status.Value.Should().NotBeNull();
        var value = status.Value;
        var msgProp = value?.GetType().GetProperty("message");
        msgProp.Should().NotBeNull();
        msgProp!.GetValue(value).Should().Be("Unexpected failure");
    }
}
