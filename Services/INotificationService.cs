using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface INotificationService
    {
        Task<PagedResult<NotificationDTO>> GetUserNotificationsAsync(int userId, int pageIndex, int pageSize);
        Task<NotificationDTO> GetNotificationByIdAsync(int notificationId, int userId);
        Task<List<NotificationDTO>> GetUnreadNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<NotificationDTO> CreateNotificationAsync(int userId, string type, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null, bool sendEmail = true, string? emailBody = null);
        Task CreateBulkNotificationsAsync(List<int> userIds, string type, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null, bool sendEmail = true, string? emailBody = null);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteNotificationAsync(int notificationId, int userId);
    }
}
