using AutoMapper;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.Extensions.Logging;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
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
                ValidateUserId(userId);
                ValidatePaging(pageIndex, pageSize);

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

        public async Task<NotificationDTO> GetNotificationByIdAsync(int notificationId, int userId)
        {
            try
            {
                ValidateUserId(userId);
                ValidateNotificationId(notificationId);

                var notification = await _repository.GetByIdAsync(notificationId);
                if (notification == null)
                {
                    throw new KeyNotFoundException($"Notification with id {notificationId} not found.");
                }

                if (notification.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User does not have permission to access this notification.");
                }

                return _mapper.Map<NotificationDTO>(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification by id. NotificationId: {NotificationId}, UserId: {UserId}", notificationId, userId);
                throw;
            }
        }

        public async Task<List<NotificationDTO>> GetUnreadNotificationsAsync(int userId)
        {
            try
            {
                ValidateUserId(userId);

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
                ValidateUserId(userId);
                return await _repository.GetUnreadCountAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unread count. UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<NotificationDTO> CreateNotificationAsync(int userId, string type, string title, string message, 
            string? relatedEntityType = null, int? relatedEntityId = null, bool sendEmail = true)
        {
            try
            {
                ValidateUserId(userId);
                var normalizedType = NormalizeNotificationType(type);
                ValidateNotificationContent(title, message, relatedEntityType, relatedEntityId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with id {userId} was not found.");
                }

                var notification = new Notification
                {
                    UserId = userId,
                    Type = normalizedType,
                    Title = title,
                    Message = message,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _repository.CreateAsync(notification);
                _logger.LogInformation("Created notification. UserId: {UserId}, Type: {Type}, Title: {Title}", 
                    userId, normalizedType, title);

                if (sendEmail)
                {
                    await TrySendNotificationEmailAsync(user.Email, title, message, userId);
                }

                return _mapper.Map<NotificationDTO>(notification);
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
                if (userIds == null || userIds.Count == 0)
                {
                    throw new ArgumentException("At least one user id is required.", nameof(userIds));
                }

                if (userIds.Any(id => id <= 0))
                {
                    throw new ArgumentException("User ids must be greater than zero.", nameof(userIds));
                }

                var normalizedType = NormalizeNotificationType(type);
                ValidateNotificationContent(title, message, relatedEntityType, relatedEntityId);

                var distinctUserIds = userIds.Distinct().ToList();
                var recipients = new List<User>(distinctUserIds.Count);

                foreach (var userId in distinctUserIds)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null)
                    {
                        throw new KeyNotFoundException($"User with id {userId} was not found.");
                    }

                    recipients.Add(user);
                }

                var createdAt = DateTime.UtcNow;
                var notifications = recipients.Select(user => new Notification
                {
                    UserId = user.UserId,
                    Type = normalizedType,
                    Title = title,
                    Message = message,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    IsRead = false,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                }).ToList();

                await _repository.CreateBulkAsync(notifications);
                _logger.LogInformation("Created bulk notifications. Count: {Count}, Type: {Type}, Title: {Title}", 
                    notifications.Count, normalizedType, title);

                if (sendEmail)
                {
                    foreach (var recipient in recipients)
                    {
                        await TrySendNotificationEmailAsync(recipient.Email, title, message, recipient.UserId);
                    }
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
                ValidateUserId(userId);
                ValidateNotificationId(notificationId);

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
                ValidateUserId(userId);
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
                ValidateUserId(userId);
                ValidateNotificationId(notificationId);

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

        private static void ValidatePaging(int pageIndex, int pageSize)
        {
            if (pageIndex <= 0)
            {
                throw new ArgumentException("Page index must be greater than zero.", nameof(pageIndex));
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));
            }
        }

        private static void ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("User id must be greater than zero.", nameof(userId));
            }
        }

        private static void ValidateNotificationId(int notificationId)
        {
            if (notificationId <= 0)
            {
                throw new ArgumentException("Notification id must be greater than zero.", nameof(notificationId));
            }
        }

        private static void ValidateNotificationContent(string title, string message, string? relatedEntityType, int? relatedEntityId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Notification title is required.", nameof(title));
            }

            if (title.Length > 255)
            {
                throw new ArgumentException("Notification title cannot exceed 255 characters.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Notification message is required.", nameof(message));
            }

            if (!string.IsNullOrWhiteSpace(relatedEntityType) && relatedEntityType.Length > 50)
            {
                throw new ArgumentException("Related entity type cannot exceed 50 characters.", nameof(relatedEntityType));
            }

            if (relatedEntityId.HasValue && relatedEntityId.Value <= 0)
            {
                throw new ArgumentException("Related entity id must be greater than zero when provided.", nameof(relatedEntityId));
            }
        }

        private static string NormalizeNotificationType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Notification type is required.", nameof(type));
            }

            if (!Enum.TryParse<NotificationType>(type, true, out var parsedType))
            {
                throw new ArgumentException($"Unsupported notification type '{type}'.", nameof(type));
            }

            return parsedType.ToString();
        }

        private async Task TrySendNotificationEmailAsync(string? toEmail, string title, string message, int userId)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogInformation("Skipped notification email because recipient email is missing. UserId: {UserId}", userId);
                return;
            }

            try
            {
                _ = new MailAddress(toEmail);
            }
            catch (FormatException)
            {
                _logger.LogWarning("Skipped notification email because recipient email is invalid. UserId: {UserId}, Email: {Email}", userId, toEmail);
                return;
            }

            try
            {
                await _emailService.SendEmailAsync(toEmail, title, message);
                _logger.LogInformation("Sent notification email. UserId: {UserId}, Email: {Email}", userId, toEmail);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send notification email. UserId: {UserId}", userId);
            }
        }
    }
}
