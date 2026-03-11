using BusinessObjects.Models;
using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface INotificationDAO
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
