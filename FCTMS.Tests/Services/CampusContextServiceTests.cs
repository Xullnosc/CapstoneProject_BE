using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Services;
using Xunit;

namespace FCTMS.Tests.Services;

public class CampusContextServiceTests
{
    [Fact]
    public void GetCurrentCampusId_UserHasCampusClaim_ReturnsCorrectId()
    {
        // Arrange
        var claims = new[] { new Claim("campus_id", "1") };
        var identity = new ClaimsIdentity(claims);
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        
        var service = new CampusContextService(mockAccessor.Object);
        
        // Act
        var result = service.GetCurrentCampusId();
        
        // Assert
        Assert.Equal(1, result);
    }
    
    [Fact]
    public void GetCurrentCampusId_SuperAdmin_ReturnsNull()
    {
        // Arrange: không có campus_id claim
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        
        var service = new CampusContextService(mockAccessor.Object);
        
        // Act
        var result = service.GetCurrentCampusId();
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentCampusId_InvalidClaim_ReturnsNull()
    {
        // Arrange: campus_id claim không phải số
        var claims = new[] { new Claim("campus_id", "ABC") };
        var identity = new ClaimsIdentity(claims);
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        
        var service = new CampusContextService(mockAccessor.Object);
        
        // Act
        var result = service.GetCurrentCampusId();
        
        // Assert
        Assert.Null(result);
    }
}
