using System.Security.Claims;
using BusinessObjects.DTOs;
using CapstoneProject_BE.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services;
using Xunit;

namespace FCTMS.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _mockDashboardService;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _mockDashboardService = new Mock<IDashboardService>();
        _controller = new DashboardController(_mockDashboardService.Object);
        SetUser(claims: new Claim(ClaimTypes.NameIdentifier, "42"));
    }

    private void SetUser(params Claim[] claims)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user },
        };
    }

    [Fact]
    public async Task GetDashboardStats_ReturnsOkWithStats()
    {
        var dto = new DashboardStatsDTO
        {
            TotalUsers = 10,
            TotalTheses = 5,
            TotalTeams = 3,
            TotalSemesters = 2,
        };
        _mockDashboardService.Setup(s => s.GetDashboardStatsAsync()).ReturnsAsync(dto);

        var result = await _controller.GetDashboardStats();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetLecturerDashboardStats_ValidUser_ReturnsOkWithStats()
    {
        SetUser(new Claim(ClaimTypes.NameIdentifier, "7"), new Claim("IsReviewer", "false"));

        var dto = new LecturerDashboardStatsDTO
        {
            CurrentSemesterCode = "SP26",
            UnreadNotifications = 2,
            CampusSummary = new DashboardStatsDTO
            {
                TotalUsers = 1,
                TotalTheses = 2,
                TotalTeams = 3,
                TotalSemesters = 4,
            },
        };

        _mockDashboardService
            .Setup(s => s.GetLecturerDashboardStatsAsync(7, false))
            .ReturnsAsync(dto);

        var result = await _controller.GetLecturerDashboardStats();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(dto);
        _mockDashboardService.Verify(s => s.GetLecturerDashboardStatsAsync(7, false), Times.Once);
    }

    [Fact]
    public async Task GetLecturerDashboardStats_IsReviewerClaim_PassesTrueToService()
    {
        SetUser(new Claim(ClaimTypes.NameIdentifier, "99"), new Claim("IsReviewer", "true"));

        var dto = new LecturerDashboardStatsDTO();
        _mockDashboardService
            .Setup(s => s.GetLecturerDashboardStatsAsync(99, true))
            .ReturnsAsync(dto);

        await _controller.GetLecturerDashboardStats();

        _mockDashboardService.Verify(s => s.GetLecturerDashboardStatsAsync(99, true), Times.Once);
    }

    [Fact]
    public async Task GetLecturerDashboardStats_MissingNameIdentifier_ReturnsUnauthorized()
    {
        SetUser(new Claim("IsReviewer", "true"));

        var result = await _controller.GetLecturerDashboardStats();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        _mockDashboardService.Verify(
            s => s.GetLecturerDashboardStatsAsync(It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetLecturerDashboardStats_InvalidNameIdentifier_ReturnsUnauthorized()
    {
        SetUser(new Claim(ClaimTypes.NameIdentifier, "not-a-number"));

        var result = await _controller.GetLecturerDashboardStats();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        _mockDashboardService.Verify(
            s => s.GetLecturerDashboardStatsAsync(It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetDashboardStats_ServiceThrows_Returns500()
    {
        _mockDashboardService
            .Setup(s => s.GetDashboardStatsAsync())
            .ThrowsAsync(new InvalidOperationException("db error"));

        var result = await _controller.GetDashboardStats();

        var obj = result.Result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetLecturerDashboardStats_ServiceThrows_Returns500()
    {
        SetUser(new Claim(ClaimTypes.NameIdentifier, "1"));
        _mockDashboardService
            .Setup(s => s.GetLecturerDashboardStatsAsync(1, false))
            .ThrowsAsync(new InvalidOperationException("db error"));

        var result = await _controller.GetLecturerDashboardStats();

        var obj = result.Result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(500);
    }
}
