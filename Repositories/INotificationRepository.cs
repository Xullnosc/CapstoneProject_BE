using BusinessObjects.DTOs;
using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface INotificationRepository
    {
        Task<PagedResult<Notification>> GetByUserIdPagedAsync(int userId, int pageIndex, int pageSize);
        Task<List<Notification>> GetUnreadByUserIdAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification?> GetByIdAsync(int notificationId);
        Task<Notification> CreateAsync(Notification notification);
        Task CreateBulkAsync(List<Notification> notifications);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAsync(int notificationId);
    }
}
