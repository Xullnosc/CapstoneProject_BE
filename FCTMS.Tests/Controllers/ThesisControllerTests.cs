using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using CapstoneProject_BE.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services;
using Xunit;

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
                new Claim(ClaimTypes.NameIdentifier, "1"),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };
        }

        [Fact]
        public async Task GetMyTheses_ShouldReturnOk_WithTheses()
        {
            // Arrange
            var dtos = new List<ThesisDTO>
            {
                new ThesisDTO { ThesisId = "1", Title = "Test 1" },
            };
            _mockThesisService
                .Setup(x => x.GetMyThesesAsync("student@fpt.edu.vn", null, null))
                .ReturnsAsync(dtos);

            // Act
            var result = await _controller.GetMyTheses(null, null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedData = okResult
                .Value.Should()
                .BeAssignableTo<IEnumerable<ThesisDTO>>()
                .Subject;
            returnedData.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetThesisDetail_ShouldReturnNotFound_WhenNull()
        {
            // Arrange
            _mockThesisService
                .Setup(x => x.GetThesisDetailAsync("invalid"))
                .ReturnsAsync((ThesisDTO?)null);

            // Act
            var result = await _controller.GetThesisDetail("invalid");

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            // value is anonymous type { Message = ... }
            notFound
                .Value!.GetType()
                .GetProperty("Message")!
                .GetValue(notFound.Value, null)
                .Should()
                .Be("Thesis with id 'invalid' not found.");
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
                new ThesisDTO { ThesisId = "1", Status = "Reviewing" },
            };
            _mockThesisService.Setup(x => x.GetFilteredThesesAsync("Reviewing", null, null, null, null, false, null, It.IsAny<string?>())).ReturnsAsync(dtos);

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
            _mockThesisService
                .Setup(x => x.UpdateThesisAsync("1", req, "student@fpt.edu.vn"))
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

            _mockThesisService
                .Setup(x => x.UpdateThesisAsync("1", req, "student@fpt.edu.vn"))
                .ReturnsAsync(returnedDto);

            // Act
            var result = await _controller.UpdateThesis("1", req);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            // anonymous type { Message, Data }
            okResult
                .Value!.GetType()
                .GetProperty("Data")!
                .GetValue(okResult.Value, null)
                .Should()
                .Be(returnedDto);
        }

        [Fact]
        public async Task SubmitReviewerDecision_ShouldReturnOk_WhenReviewerAndValidStatus()
        {
            // Arrange: set up user with IsReviewer claim
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "reviewer@fpt.edu.vn"),
                new Claim(ClaimTypes.NameIdentifier, "2"), // reviewer userId
                new Claim("IsReviewer", "true")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            };

            var dto = new SubmitThesisDecisionDTO { Decision = "Pass" };
            var returnedResult = new ThesisReviewStatusDTO { OverallStatus = "Published" };
            
            _mockThesisService.Setup(x => x.SubmitReviewerDecisionAsync("guid-1", 2, dto))
                .ReturnsAsync(returnedResult);

            // Act
            var result = await _controller.SubmitReviewerDecision("guid-1", dto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(returnedResult);
            _mockThesisService.Verify(x => x.SubmitReviewerDecisionAsync("guid-1", 2, dto), Times.Once);
        }

        [Fact]
        public async Task SubmitReviewerDecision_ShouldReturnBadRequest_WhenArgumentInvalid()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, "r@fpt.edu.vn"), new Claim(ClaimTypes.NameIdentifier, "2") };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
                },
            };

            var dto = new SubmitThesisDecisionDTO { Decision = "Invalid" };
            _mockThesisService.Setup(x => x.SubmitReviewerDecisionAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SubmitThesisDecisionDTO>()))
                .ThrowsAsync(new ArgumentException("Invalid decision"));

            var result = await _controller.SubmitReviewerDecision("guid-1", dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SubmitReviewerDecision_ShouldReturnNotFound_WhenThesisNotFound()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, "r@fpt.edu.vn"), new Claim(ClaimTypes.NameIdentifier, "2") };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
                },
            };

            _mockThesisService.Setup(x => x.SubmitReviewerDecisionAsync("missing", 2, It.IsAny<SubmitThesisDecisionDTO>()))
                .ThrowsAsync(new KeyNotFoundException("Thesis not found"));

            var result = await _controller.SubmitReviewerDecision("missing", new SubmitThesisDecisionDTO { Decision = "Fail", Comment = "Reason" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task AssignReviewers_ShouldReturnOk_WhenServiceReturnsStatus()
        {
            // Arrange
            var thesisId = "guid-assign-1";
            var dto = new AssignThesisReviewersDTO { ReviewerIds = new[] { 10, 20 } };

            var returnedStatus = new ThesisReviewStatusDTO { OverallStatus = "Pass" };

            _mockThesisService
                .Setup(x => x.AssignReviewersAsync(
                    thesisId,
                    It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10, 20 })),
                    1))
                .ReturnsAsync(returnedStatus);

            // Act
            var result = await _controller.AssignReviewers(thesisId, dto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(returnedStatus);
            _mockThesisService.Verify(x => x.AssignReviewersAsync(
                thesisId,
                It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10, 20 })),
                1
            ), Times.Once);
        }

        [Fact]
        public async Task AssignReviewers_ShouldReturnBadRequest_WhenReviewerIdsEmpty()
        {
            // Arrange
            var thesisId = "guid-assign-2";
            var dto = new AssignThesisReviewersDTO { ReviewerIds = Array.Empty<int>() };

            // Act
            var result = await _controller.AssignReviewers(thesisId, dto);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().NotBeNull();
            badRequest.Value!.GetType().GetProperty("Message")!.GetValue(badRequest.Value, null)
                .Should().Be("ReviewerIds is required.");
        }

        // ─── F105: ForceAssignThesis Tests ───────────────────────────────────────

        [Fact]
        public async Task ForceAssignThesis_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var dto = new ForceAssignThesisDTO { TeamId = 10 };
            var returnedDto = new ThesisDTO { ThesisId = "thesis-1", Status = "Registered" };
            _mockThesisService
                .Setup(x => x.ForceAssignThesisAsync("thesis-1", 10, 1))
                .ReturnsAsync(returnedDto);

            // Act
            var result = await _controller.ForceAssignThesis("thesis-1", dto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value!.GetType().GetProperty("Data")!
                .GetValue(okResult.Value, null).Should().Be(returnedDto);
        }

        [Fact]
        public async Task ForceAssignThesis_ShouldReturnNotFound_WhenThesisNotFound()
        {
            // Arrange
            var dto = new ForceAssignThesisDTO { TeamId = 10 };
            _mockThesisService
                .Setup(x => x.ForceAssignThesisAsync("missing", 10, 1))
                .ThrowsAsync(new KeyNotFoundException("Thesis 'missing' not found."));

            // Act
            var result = await _controller.ForceAssignThesis("missing", dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ForceAssignThesis_ShouldReturnForbidden_WhenNotHod()
        {
            // Arrange
            var dto = new ForceAssignThesisDTO { TeamId = 10 };
            _mockThesisService
                .Setup(x => x.ForceAssignThesisAsync("thesis-1", 10, 1))
                .ThrowsAsync(new UnauthorizedAccessException("Only Head of Department can force-assign theses."));

            // Act
            var result = await _controller.ForceAssignThesis("thesis-1", dto);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task ForceAssignThesis_ShouldReturnBadRequest_WhenThesisNotPublished()
        {
            // Arrange
            var dto = new ForceAssignThesisDTO { TeamId = 10 };
            _mockThesisService
                .Setup(x => x.ForceAssignThesisAsync("thesis-1", 10, 1))
                .ThrowsAsync(new InvalidOperationException("Thesis must be 'Published' to force-assign."));

            // Act
            var result = await _controller.ForceAssignThesis("thesis-1", dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProposeThesis_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var dto = new ProposeThesisDTO { Title = "New Thesis" };
            // ProposeThesisAsync returns Task<Thesis>, not Task<ThesisDTO>
            var returnedThesis = new Thesis { ThesisId = "t-new", Status = "On Mentor Inviting" };
            _mockThesisService
                .Setup(x => x.ProposeThesisAsync(It.IsAny<ProposeThesisDTO>(), "student@fpt.edu.vn"))
                .ReturnsAsync(returnedThesis);

            // Act
            var result = await _controller.ProposeThesis(dto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task ProposeThesis_ShouldReturnBadRequest_WhenRegistrationClosed()
        {
            // Arrange
            var dto = new ProposeThesisDTO { Title = "Thesis" };
            _mockThesisService
                .Setup(x => x.ProposeThesisAsync(It.IsAny<ProposeThesisDTO>(), "student@fpt.edu.vn"))
                .ThrowsAsync(new InvalidOperationException("Thesis registration is closed."));

            // Act
            var result = await _controller.ProposeThesis(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProposeThesis_ShouldReturnForbidden_WhenNotTeamLeader()
        {
            // Arrange
            var dto = new ProposeThesisDTO { Title = "Thesis" };
            _mockThesisService
                .Setup(x => x.ProposeThesisAsync(It.IsAny<ProposeThesisDTO>(), "student@fpt.edu.vn"))
                .ThrowsAsync(new UnauthorizedAccessException("Only the team leader can propose."));

            // Act
            var result = await _controller.ProposeThesis(dto);

            // Assert
            var statusResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            statusResult.Value!.ToString().Should().Contain("Only the team leader can propose");
        }

        [Fact]
        public async Task CancelThesis_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            string thesisId = "t-cancel";
            var returnedDto = new ThesisDTO { ThesisId = thesisId, Status = "Cancelled" };
            _mockThesisService
                .Setup(x => x.CancelThesisAsync(thesisId, "student@fpt.edu.vn"))
                .ReturnsAsync(returnedDto);

            // Act
            var result = await _controller.CancelThesis(thesisId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CancelThesis_ShouldReturnNotFound_WhenThesisMissing()
        {
            // Arrange
            _mockThesisService
                .Setup(x => x.CancelThesisAsync("missing", "student@fpt.edu.vn"))
                .ThrowsAsync(new KeyNotFoundException("Thesis not found."));

            // Act
            var result = await _controller.CancelThesis("missing");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CancelThesis_ShouldReturnForbidden_WhenNotOwner()
        {
            // Arrange
            _mockThesisService
                .Setup(x => x.CancelThesisAsync("t-1", "student@fpt.edu.vn"))
                .ThrowsAsync(new UnauthorizedAccessException("You are not authorized."));

            // Act
            var result = await _controller.CancelThesis("t-1");

            // Assert
            var objResult = result.Should().BeOfType<ObjectResult>().Subject;
            objResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CancelThesis_ShouldReturnBadRequest_WhenInvalidOperation()
        {
            // Arrange
            _mockThesisService
                .Setup(x => x.CancelThesisAsync("t-pub", "student@fpt.edu.vn"))
                .ThrowsAsync(new InvalidOperationException("Cannot cancel a Published thesis."));

            // Act
            var result = await _controller.CancelThesis("t-pub");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetAllTheses_ShouldReturnOk_WithEmptyList()
        {
            // Arrange
            _mockThesisService
                .Setup(x => x.GetFilteredThesesAsync(
                    null, null, null, null, null, false, It.IsAny<int?>(), It.IsAny<string?>()))
                .ReturnsAsync(new List<ThesisDTO>());

            // Act
            var result = await _controller.GetAllTheses(null, null, null, null, null, false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var data = okResult.Value.Should().BeAssignableTo<IEnumerable<ThesisDTO>>().Subject;
            data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyTheses_ShouldReturnOk_WithEmptyList_WhenNoTheses()
        {
            // Arrange
            _mockThesisService
                .Setup(x => x.GetMyThesesAsync("student@fpt.edu.vn", null, null))
                .ReturnsAsync(new List<ThesisDTO>());

            // Act
            var result = await _controller.GetMyTheses(null, null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var data = okResult.Value.Should().BeAssignableTo<IEnumerable<ThesisDTO>>().Subject;
            data.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateThesis_ShouldReturnBadRequest_WhenInvalidOperation()
        {
            // Arrange
            var req = new UpdateThesisDTO { Title = "Updated" };
            _mockThesisService
                .Setup(x => x.UpdateThesisAsync("t-x", req, "student@fpt.edu.vn"))
                .ThrowsAsync(new InvalidOperationException("Thesis cannot be updated in current state."));

            // Act
            var result = await _controller.UpdateThesis("t-x", req);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateThesis_ShouldReturnNotFound_WhenThesisMissing()
        {
            // Arrange
            var req = new UpdateThesisDTO { Title = "X" };
            _mockThesisService
                .Setup(x => x.UpdateThesisAsync("missing-t", req, "student@fpt.edu.vn"))
                .ThrowsAsync(new KeyNotFoundException("Thesis not found."));

            // Act
            var result = await _controller.UpdateThesis("missing-t", req);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetReviewStatus_ShouldReturnOk_WhenThesisExists()
        {
            // Arrange
            string thesisId = "t-review";
            var status = new ThesisReviewStatusDTO
            {
                OverallStatus = "Pass",
                Reviewers = new List<ReviewerProgressDTO>()
            };
            _mockThesisService.Setup(x => x.GetReviewStatusAsync(thesisId)).ReturnsAsync(status);

            // Act
            var result = await _controller.GetReviewStatus(thesisId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(status);
        }

        [Fact]
        public async Task GetReviewStatus_ShouldReturnNotFound_WhenThesisMissing()
        {
            // Arrange
            _mockThesisService
                .Setup(x => x.GetReviewStatusAsync("no-t"))
                .ThrowsAsync(new KeyNotFoundException("Thesis not found."));

            // Act
            var result = await _controller.GetReviewStatus("no-t");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}

