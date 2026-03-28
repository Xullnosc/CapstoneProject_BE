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
    }
}
