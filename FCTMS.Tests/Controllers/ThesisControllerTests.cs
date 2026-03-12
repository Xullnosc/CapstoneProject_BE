using BusinessObjects.DTOs;
using BusinessObjects.Models;
using CapstoneProject_BE.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Linq;

namespace FCTMS.Tests.Controllers
{
    public class ThesisControllerTests
    {
        private readonly Mock<IThesisService> _mockThesisService;
        private readonly ThesisController _controller;

        public ThesisControllerTests()
        {
            _mockThesisService = new Mock<IThesisService>();
            _controller = new ThesisController(_mockThesisService.Object);

            // Mock claims for the controller
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "student@fpt.edu.vn"),
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetMyTheses_ShouldReturnOk_WithTheses()
        {
            // Arrange
            var dtos = new List<ThesisDTO>
            {
                new ThesisDTO { ThesisId = "1", Title = "Test 1" }
            };
            _mockThesisService.Setup(x => x.GetMyThesesAsync("student@fpt.edu.vn", null, null)).ReturnsAsync(dtos);

            // Act
            var result = await _controller.GetMyTheses(null, null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedData = okResult.Value.Should().BeAssignableTo<IEnumerable<ThesisDTO>>().Subject;
            returnedData.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetThesisDetail_ShouldReturnNotFound_WhenNull()
        {
            // Arrange
            _mockThesisService.Setup(x => x.GetThesisDetailAsync("invalid")).ReturnsAsync((ThesisDTO?)null);

            // Act
            var result = await _controller.GetThesisDetail("invalid");

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            // value is anonymous type { Message = ... }
            notFound.Value!.GetType().GetProperty("Message")!.GetValue(notFound.Value, null)
                .Should().Be("Thesis with id 'invalid' not found.");
        }

        [Fact]
        public async Task GetThesisDetail_ShouldReturnOk_WhenFound()
        {
            // Arrange
            var dto = new ThesisDTO { ThesisId = "1", Title = "Test" };
            _mockThesisService.Setup(x => x.GetThesisDetailAsync("1")).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetThesisDetail("1");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(dto);
        }

        [Fact]
        public async Task GetAllTheses_ShouldReturnOk_WithData()
        {
            // Arrange
            var dtos = new List<ThesisDTO>
            {
                new ThesisDTO { ThesisId = "1", Status = "Reviewing" }
            };
            _mockThesisService.Setup(x => x.GetFilteredThesesAsync("Reviewing", null, null, null, null, false, null)).ReturnsAsync(dtos);

            // Act
            var result = await _controller.GetAllTheses("Reviewing", null, null, null, null, false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeAssignableTo<IEnumerable<ThesisDTO>>();
        }

        [Fact]
        public async Task UpdateThesis_ShouldReturnForbidden_WhenUnauthorizedAccess()
        {
            // Arrange
            var req = new UpdateThesisDTO { Title = "Test" };
            _mockThesisService.Setup(x => x.UpdateThesisAsync("1", req, "student@fpt.edu.vn"))
                .ThrowsAsync(new UnauthorizedAccessException("Not allowed"));

            // Act
            var result = await _controller.UpdateThesis("1", req);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task UpdateThesis_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var req = new UpdateThesisDTO { Title = "Test" };
            var returnedDto = new ThesisDTO { ThesisId = "1", Title = "Test" };
            
            _mockThesisService.Setup(x => x.UpdateThesisAsync("1", req, "student@fpt.edu.vn"))
                .ReturnsAsync(returnedDto);

            // Act
            var result = await _controller.UpdateThesis("1", req);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            // anonymous type { Message, Data }
            okResult.Value!.GetType().GetProperty("Data")!.GetValue(okResult.Value, null)
                .Should().Be(returnedDto);
        }

        [Fact]
        public async Task ReviewThesis_ShouldReturnOk_WhenReviewerAndValidStatus()
        {
            // Arrange: set up user with IsReviewer claim so [Authorize(Policy = "Reviewer")] passes
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "reviewer@fpt.edu.vn"),
                new Claim("IsReviewer", "true")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            var dto = new ReviewSubmissionDTO { Status = "Approve", Comment = "Looks good" };
            var returnedDto = new ThesisDTO { Status = "Reviewing" };
            _mockThesisService.Setup(x => x.SubmitReviewAsync("guid-1", dto, "reviewer@fpt.edu.vn")).ReturnsAsync(returnedDto);

            // Act
            var result = await _controller.ReviewThesis("guid-1", dto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _mockThesisService.Verify(x => x.SubmitReviewAsync("guid-1", dto, "reviewer@fpt.edu.vn"), Times.Once);
        }

        [Fact]
        public async Task ReviewThesis_ShouldReturnBadRequest_WhenInvalidStatus()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, "r@fpt.edu.vn"), new Claim("IsReviewer", "true") };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
            };

            var result = await _controller.ReviewThesis("guid-1", new ReviewSubmissionDTO { Status = "Invalid" });

            result.Should().BeOfType<BadRequestObjectResult>();
            _mockThesisService.Verify(x => x.SubmitReviewAsync(It.IsAny<string>(), It.IsAny<ReviewSubmissionDTO>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReviewThesis_ShouldReturnNotFound_WhenThesisNotFound()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, "r@fpt.edu.vn"), new Claim("IsReviewer", "true") };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
            };

            _mockThesisService.Setup(x => x.SubmitReviewAsync("missing", It.IsAny<ReviewSubmissionDTO>(), "r@fpt.edu.vn"))
                .ThrowsAsync(new KeyNotFoundException("Thesis not found"));

            var result = await _controller.ReviewThesis("missing", new ReviewSubmissionDTO { Status = "Reject", Comment = "Bad" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
