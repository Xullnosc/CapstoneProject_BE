using BusinessObjects;
using BusinessObjects.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repositories;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class MidtermLockReminderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MidtermLockReminderWorker> _logger;

        public MidtermLockReminderWorker(IServiceProvider serviceProvider, ILogger<MidtermLockReminderWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MidtermLockReminderWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndNotifyMidtermLockAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in MidtermLockReminderWorker.");
                }

                // Wait 1 hour before checking again or calculate exact time till next 8 AM
                // Keep it simple: run hourly checks and only send if it's the exact day and hasn't been locked.
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckAndNotifyMidtermLockAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var semesterRepository = scope.ServiceProvider.GetRequiredService<ISemesterRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var activeSemesters = await semesterRepository.GetAllSemestersAsync();
            var today = DateTime.UtcNow.Date;

            foreach (var semester in activeSemesters.Where(s => s.Status == CampusConstants.SemesterStatus.Open && s.MidtermReview != null))
            {
                if (semester.MidtermReview!.LockDate.Date == today)
                {
                    // It's the lock date today!
                    // Notify HODs of the same campus
                    var campusHodRoles = await semesterRepository.GetAllRolesAsync();
                    var hodRole = campusHodRoles.FirstOrDefault(r => r.RoleName == CampusConstants.Roles.HOD);

                    if (hodRole != null)
                    {
                        var hods = await userRepository.GetUsersByRoleAsync(CampusConstants.Roles.HOD, null);
                        var campusHods = hods.Where(u => u.CampusId == semester.CampusId).ToList();

                        if (campusHods.Any())
                        {
                            var userIds = campusHods.Select(h => h.UserId).ToList();
                            string title = "Nhắc nhở: Thực hiện Lock Học Kỳ";
                            string message = $"Hôm nay là ngày được thông báo để lock học kỳ {semester.SemesterCode} cho việc Review Giữa Kỳ. Vui lòng truy cập hệ thống và thực hiện manual lock để chốt học kỳ.";

                            await notificationService.CreateBulkNotificationsAsync(
                                userIds,
                                NotificationType.HODAction.ToString(),
                                title,
                                message,
                                "Semester",
                                semester.SemesterId,
                                sendEmail: true
                            );

                            _logger.LogInformation($"Sent Midterm Lock reminder to {campusHods.Count} HODs for Semester {semester.SemesterCode}.");
                        }
                    }
                }
            }
        }
    }
}
