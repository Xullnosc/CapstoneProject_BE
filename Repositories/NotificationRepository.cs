using BusinessObjects.DTOs;
using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly INotificationDAO _dao;

        public NotificationRepository(INotificationDAO dao)
        {
            _dao = dao;
        }

        public async Task<PagedResult<Notification>> GetByUserIdPagedAsync(int userId, int pageIndex, int pageSize)
        {
            return await _dao.GetByUserIdPagedAsync(userId, pageIndex, pageSize);
        }

        public async Task<List<Notification>> GetUnreadByUserIdAsync(int userId)
        {
            return await _dao.GetUnreadByUserIdAsync(userId);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _dao.GetUnreadCountAsync(userId);
        }

        public async Task<Notification?> GetByIdAsync(int notificationId)
        {
            return await _dao.GetByIdAsync(notificationId);
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            return await _dao.CreateAsync(notification);
        }

        public async Task CreateBulkAsync(List<Notification> notifications)
        {
            await _dao.CreateBulkAsync(notifications);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            await _dao.MarkAsReadAsync(notificationId);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _dao.MarkAllAsReadAsync(userId);
        }

        public async Task DeleteAsync(int notificationId)
        {
            await _dao.DeleteAsync(notificationId);
        }
    }
}
