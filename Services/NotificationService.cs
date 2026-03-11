using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.Extensions.Logging;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repository,
            IUserRepository userRepository,
            IEmailService emailService,
            IMapper mapper,
            ILogger<NotificationService> logger)
        {
            _repository = repository;
            _userRepository = userRepository;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<NotificationDTO>> GetUserNotificationsAsync(int userId, int pageIndex, int pageSize)
        {
            try
            {
                var pagedResult = await _repository.GetByUserIdPagedAsync(userId, pageIndex, pageSize);
                var dtos = _mapper.Map<List<NotificationDTO>>(pagedResult.Items);
                return new PagedResult<NotificationDTO>(dtos, pagedResult.TotalCount, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user notifications. UserId: {UserId}, Page: {PageIndex}", userId, pageIndex);
                throw;
            }
        }

        public async Task<List<NotificationDTO>> GetUnreadNotificationsAsync(int userId)
        {
            try
            {
                var notifications = await _repository.GetUnreadByUserIdAsync(userId);
                return _mapper.Map<List<NotificationDTO>>(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unread notifications. UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _repository.GetUnreadCountAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unread count. UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task CreateNotificationAsync(int userId, string type, string title, string message, 
            string? relatedEntityType = null, int? relatedEntityId = null, bool sendEmail = true)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                // Create notification in database first
                await _repository.CreateAsync(notification);
                _logger.LogInformation("Created notification. UserId: {UserId}, Type: {Type}, Title: {Title}", 
                    userId, type, title);

                // Send email notification asynchronously (fire-and-forget, graceful degradation)
                if (sendEmail)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var user = await _userRepository.GetByIdAsync(userId);
                            if (user != null && !string.IsNullOrEmpty(user.Email))
                            {
                                await _emailService.SendEmailAsync(user.Email, title, message);
                                _logger.LogInformation("Sent notification email. UserId: {UserId}, Email: {Email}", 
                                    userId, user.Email);
                            }
                        }
                        catch (Exception emailEx)
                        {
                            // Log error but don't fail notification creation
                            _logger.LogError(emailEx, "Failed to send notification email. UserId: {UserId}", userId);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification. UserId: {UserId}, Type: {Type}", userId, type);
                throw;
            }
        }

        public async Task CreateBulkNotificationsAsync(List<int> userIds, string type, string title, string message, 
            string? relatedEntityType = null, int? relatedEntityId = null, bool sendEmail = true)
        {
            try
            {
                var notifications = userIds.Select(userId => new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }).ToList();

                // Bulk create notifications in database
                await _repository.CreateBulkAsync(notifications);
                _logger.LogInformation("Created bulk notifications. Count: {Count}, Type: {Type}, Title: {Title}", 
                    userIds.Count, type, title);

                // Send emails asynchronously (fire-and-forget)
                if (sendEmail)
                {
                    _ = Task.Run(async () =>
                    {
                        foreach (var userId in userIds)
                        {
                            try
                            {
                                var user = await _userRepository.GetByIdAsync(userId);
                                if (user != null && !string.IsNullOrEmpty(user.Email))
                                {
                                    await _emailService.SendEmailAsync(user.Email, title, message);
                                }
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "Failed to send bulk notification email. UserId: {UserId}", userId);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create bulk notifications. UserCount: {Count}, Type: {Type}", userIds.Count, type);
                throw;
            }
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                // Validate ownership
                var notification = await _repository.GetByIdAsync(notificationId);
                if (notification == null)
                {
                    throw new KeyNotFoundException($"Notification with id {notificationId} not found.");
                }

                if (notification.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User does not have permission to mark this notification as read.");
                }

                await _repository.MarkAsReadAsync(notificationId);
                _logger.LogInformation("Marked notification as read. NotificationId: {NotificationId}, UserId: {UserId}", 
                    notificationId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification as read. NotificationId: {NotificationId}, UserId: {UserId}", 
                    notificationId, userId);
                throw;
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            try
            {
                await _repository.MarkAllAsReadAsync(userId);
                _logger.LogInformation("Marked all notifications as read. UserId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications as read. UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                // Validate ownership
                var notification = await _repository.GetByIdAsync(notificationId);
                if (notification == null)
                {
                    throw new KeyNotFoundException($"Notification with id {notificationId} not found.");
                }

                if (notification.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User does not have permission to delete this notification.");
                }

                await _repository.DeleteAsync(notificationId);
                _logger.LogInformation("Deleted notification. NotificationId: {NotificationId}, UserId: {UserId}", 
                    notificationId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete notification. NotificationId: {NotificationId}, UserId: {UserId}", 
                    notificationId, userId);
                throw;
            }
        }
    }
}
