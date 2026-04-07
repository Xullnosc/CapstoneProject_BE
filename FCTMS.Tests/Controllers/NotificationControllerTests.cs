using BusinessObjects;
using BusinessObjects.DTOs;
using CapstoneProject_BE.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services;
using System.Collections.Generic;
using System.Security.Claims;

namespace FCTMS.Tests.Controllers
{
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _controller = new NotificationController(_mockNotificationService.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, CampusConstants.Roles.Student)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetMyNotifications_ReturnsOk_WithPagedResult()
        {
            var resultSet = new PagedResult<NotificationDTO>(new List<NotificationDTO>
            {
                new NotificationDTO { NotificationId = 10, UserId = 1, Title = "New alert", Type = "SystemAnnouncement", Message = "Hello" }
            }, 1, 1, 10);

            _mockNotificationService
                .Setup(service => service.GetUserNotificationsAsync(1, 1, 10))
                .ReturnsAsync(resultSet);

            var result = await _controller.GetMyNotifications();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<PagedResult<NotificationDTO>>(okResult.Value);
            Assert.Single(value.Items);
        }

        [Fact]
        public async Task GetNotificationById_ReturnsOk_WhenFound()
        {
            var dto = new NotificationDTO { NotificationId = 9, UserId = 1, Title = "Title", Type = "TeamInvitation", Message = "Message" };

            _mockNotificationService
                .Setup(service => service.GetNotificationByIdAsync(9, 1))
                .ReturnsAsync(dto);

            var result = await _controller.GetNotificationById(9);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, okResult.Value);
        }

        [Fact]
        public async Task CreateNotification_ReturnsCreatedAtAction_WhenSuccess()
        {
            var request = new NotificationCreateDTO
            {
                UserId = 5,
                Type = "SystemAnnouncement",
                Title = "System update",
                Message = "Maintenance tonight",
                SendEmail = false
            };

            var pagedResult = new PagedResult<NotificationDTO>(new List<NotificationDTO>
            {
                new NotificationDTO { NotificationId = 99, UserId = 5, Title = request.Title, Type = request.Type, Message = request.Message }
            }, 1, 1, 1);

            _mockNotificationService
                .Setup(service => service.CreateNotificationAsync(5, request.Type, request.Title, request.Message, request.RelatedEntityType, request.RelatedEntityId, request.SendEmail))
                .ReturnsAsync(pagedResult.Items[0]);

            var result = await _controller.CreateNotification(request);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(NotificationController.GetNotificationById), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateBulkNotifications_ReturnsOk_WhenSuccess()
        {
            var request = new BulkNotificationCreateDTO
            {
                UserIds = new List<int> { 1, 2, 3 },
                Type = "ChecklistUpdate",
                Title = "Checklist changed",
                Message = "Review the latest checklist",
                SendEmail = false
            };

            _mockNotificationService
                .Setup(service => service.CreateBulkNotificationsAsync(request.UserIds, request.Type, request.Title, request.Message, request.RelatedEntityType, request.RelatedEntityId, request.SendEmail, It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateBulkNotifications(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task MarkAsRead_ReturnsOk_WhenSuccess()
        {
            _mockNotificationService
                .Setup(service => service.MarkAsReadAsync(3, 1))
                .Returns(Task.CompletedTask);

            var result = await _controller.MarkAsRead(3);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteNotification_ReturnsNoContent_WhenSuccess()
        {
            _mockNotificationService
                .Setup(service => service.DeleteNotificationAsync(7, 1))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteNotification(7);

            Assert.IsType<NoContentResult>(result);
        }
    }
}