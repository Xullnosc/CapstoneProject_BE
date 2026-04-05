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

namespace FCTMS.Tests.Controllers
{
    public class ChecklistControllerTests
    {
        private readonly Mock<IChecklistService> _mockService;
        private readonly ChecklistController _controller;

        public ChecklistControllerTests()
        {
            _mockService = new Mock<IChecklistService>();
            _controller = new ChecklistController(_mockService.Object);
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, "user@fpt.edu.vn"), new Claim(ClaimTypes.NameIdentifier, "1") };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
            };
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithList()
        {
            var list = new List<ChecklistDTO>
            {
                new ChecklistDTO { ChecklistId = 1, Content = "Content 1" }
            };
            _mockService.Setup(x => x.GetAllAsync()).ReturnsAsync(list);

            var result = await _controller.GetAll();

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value.Should().BeAssignableTo<IEnumerable<ChecklistDTO>>().Subject;
            value.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenNull()
        {
            _mockService.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((ChecklistDTO?)null);

            var result = await _controller.GetById(999);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenFound()
        {
            var dto = new ChecklistDTO { ChecklistId = 1, Content = "C" };
            _mockService.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(dto);

            var result = await _controller.GetById(1);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(dto);
        }

        [Fact]
        public async Task Update_ShouldReturnNoContent_WhenSuccess()
        {
            var dto = new ChecklistUpdateDTO { Content = "C" };
            _mockService.Setup(x => x.UpdateAsync(1, dto)).Returns(Task.CompletedTask);

            var result = await _controller.Update(1, dto);

            result.Should().BeOfType<NoContentResult>();
            _mockService.Verify(x => x.UpdateAsync(1, dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenKeyNotFoundException()
        {
            var dto = new ChecklistUpdateDTO { Content = "C" };
            _mockService.Setup(x => x.UpdateAsync(999, dto)).ThrowsAsync(new KeyNotFoundException("Checklist with id 999 not found."));

            var result = await _controller.Update(999, dto);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenSuccess()
        {
            _mockService.Setup(x => x.DeleteAsync(1)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(1);

            result.Should().BeOfType<NoContentResult>();
            _mockService.Verify(x => x.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenKeyNotFoundException()
        {
            _mockService.Setup(x => x.DeleteAsync(999)).ThrowsAsync(new KeyNotFoundException("Not found."));

            var result = await _controller.Delete(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetAll_ShouldReturnOkWithEmptyList_WhenNoChecklistsExist()
        {
            // Arrange
            // Service returns an empty list â€” no checklist items in the system.
            _mockService.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ChecklistDTO>());

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value.Should().BeAssignableTo<IEnumerable<ChecklistDTO>>().Subject;
            // The list must be empty, not null.
            value.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAll_ShouldReturnAllItems_WhenMultipleChecklistsExist()
        {
            // Arrange
            // Five checklist milestones for a typical capstone project semester.
            var checklists = new List<ChecklistDTO>
            {
                new ChecklistDTO { ChecklistId = 1, Content = "Topic Selection Complete" },
                new ChecklistDTO { ChecklistId = 2, Content = "First Mentor Meeting Done" },
                new ChecklistDTO { ChecklistId = 3, Content = "Proposal Submitted" },
                new ChecklistDTO { ChecklistId = 4, Content = "Midterm Report Approved" },
                new ChecklistDTO { ChecklistId = 5, Content = "Final Presentation Scheduled" }
            };
            _mockService.Setup(x => x.GetAllAsync()).ReturnsAsync(checklists);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value.Should().BeAssignableTo<IEnumerable<ChecklistDTO>>().Subject;
            // All 5 milestones must be present.
            value.Should().HaveCount(5);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenChecklistCreatedSuccessfully()
        {
            // Arrange
            // The DTO representing the new checklist item to create.
            var createDto = new ChecklistCreateDTO { Content = "Submit Final Thesis PDF" };
            // The DTO returned by the service after the item is persisted.
            var resultDto = new ChecklistDTO { ChecklistId = 10, Content = "Submit Final Thesis PDF" };

            _mockService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(resultDto);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            // The controller should return 201 Created with the created resource in the body.
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Value.Should().BeEquivalentTo(resultDto);
            // Verify the service was called exactly once.
            _mockService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenUnexpectedExceptionOccurs()
        {
            // Arrange
            var dto = new ChecklistUpdateDTO { Content = "Updated content" };
            // Simulates an unexpected database or network error.
            _mockService.Setup(x => x.UpdateAsync(1, dto))
                .ThrowsAsync(new Exception("Unexpected database failure"));

            // Act
            var result = await _controller.Update(1, dto);

            // Assert
            // The controller directly returns a BadRequest with the exception message
            var errorResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Delete_ShouldReturnBadRequest_WhenUnexpectedExceptionOccurs()
        {
            // Arrange
            // Simulates a low-level database exception during deletion.
            _mockService.Setup(x => x.DeleteAsync(5))
                .ThrowsAsync(new Exception("Foreign key constraint violation"));

            // Act
            var result = await _controller.Delete(5);

            // Assert
            // Controller handles generic Exception returning BadRequest
            var errorResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetById_ShouldThrow_WhenDatabaseThrows()
        {
            // Arrange
            _mockService.Setup(x => x.GetByIdAsync(1))
                .ThrowsAsync(new Exception("Connection pool exhausted"));

            // Act
            Func<Task> act = async () => await _controller.GetById(1);

            // Assert
            // GetById has no try-catch, so the exception propagates to the caller.
            await act.Should().ThrowAsync<Exception>().WithMessage("Connection pool exhausted");
        }
    }
}
