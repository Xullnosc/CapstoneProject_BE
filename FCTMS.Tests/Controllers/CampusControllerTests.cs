using System.Security.Claims;
using BusinessObjects;
using BusinessObjects.DTOs;
using CapstoneProject_BE.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FCTMS.Tests.Controllers;

public class CampusControllerTests
{
    private readonly Mock<ICampusService> _mockCampusService;
    private readonly CampusController _controller;

    public CampusControllerTests()
    {
        _mockCampusService = new Mock<ICampusService>();
        _controller = new CampusController(_mockCampusService.Object);

        // Default User (Admin role for most tests)
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, CampusConstants.Roles.Admin)
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetAll_ValidRequest_ReturnsOk()
    {
        // Arrange
        var campuses = new List<CampusDTO> 
        { 
            new CampusDTO { CampusId = 1, CampusCode = "DN", CampusName = "Đà Nẵng" } 
        };
        _mockCampusService.Setup(s => s.GetAllCampusesAsync()).ReturnsAsync(campuses);

        // Act
        var result = await _controller.GetAllCampuses();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(campuses);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateCampusDTO { CampusCode = "HN", CampusName = "Hà Nội" };
        var created = new CampusDTO { CampusId = 2, CampusCode = "HN", CampusName = "FU-Hòa Lạc" };
        _mockCampusService.Setup(s => s.CreateCampusAsync(dto)).ReturnsAsync(created);

        // Act
        var result = await _controller.CreateCampus(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task GetById_NotExists_ReturnsNotFound()
    {
        // Arrange
        _mockCampusService.Setup(s => s.GetCampusByIdAsync(99)).ReturnsAsync((CampusDTO?)null);

        // Act
        var result = await _controller.GetCampusById(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_BusyCampus_ReturnsBadRequest()
    {
        // Arrange
        _mockCampusService.Setup(s => s.DeleteCampusAsync(1))
            .ThrowsAsync(new InvalidOperationException("Cannot delete this campus because it has active references"));

        // Act
        var result = await _controller.DeleteCampus(1);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeEquivalentTo(new { message = "Cannot delete this campus because it has active references" });
    }
}
