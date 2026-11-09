using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace SingularSystem.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _appDbContext;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(AppDbContext appDbContext, ILogger<NotificationService> logger, IHubContext<NotificationHub> hubContext, UserManager<ApplicationUser> userManager)
        {
            _appDbContext = appDbContext;
            _hubContext = hubContext;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task CreateNotification(string userId, TypeNotification type, string title, string message, string actionURL)
        {
            try
            {
                var notification = new Notification()
                {
                    UserID = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    ActionURL = actionURL
                };

                await _appDbContext.Notifications.AddAsync(notification);
                await _appDbContext.SaveChangesAsync();

                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
                {
                    userId,
                    title,
                    message,
                    actionURL
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task CreateNotificationAdmins(TypeNotification type, string title, string message, string actionURL)
        {
            try
            {
                var admins = await _userManager.GetUsersInRoleAsync("admin");

                foreach (var admin in admins)
                {
                    await CreateNotification(admin.Id, type, title, message, actionURL);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task MarkAsRead(Guid idNotification)
        {
            try
            {
                var notification = await _appDbContext.Notifications.FirstOrDefaultAsync(temp => temp.ID == idNotification);

                if (notification != null && !notification.Read)
                {
                    notification.Read = true;
                    notification.ReadAt = DateTime.UtcNow;

                    await _appDbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task MarkAllAsRead(string userId)
        {
            try
            {
                var notifications = await _appDbContext.Notifications.Where(temp => temp.UserID == userId).ToListAsync();

                foreach (var notification in notifications)
                {
                    if (!notification.Read)
                    {
                        notification.Read = true;
                        notification.ReadAt = DateTime.UtcNow;
                    }
                }
                await _appDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task<List<Notification>> GetNotificationsFromUser(Guid idUser, bool includeReads = false)
        {
            try
            {
                var notifications = _appDbContext.Notifications.Where(temp => temp.UserID == idUser.ToString());

                if (!includeReads)
                {
                    notifications = notifications.Where(temp => !temp.Read);
                }

                return await notifications.OrderByDescending(temp => temp.CreatedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }
    }

    public class NotificationHub : Hub
    {
        //public override Task OnConnectedAsync()
        //{
        //    var userId = Context.UserIdentifier;
        //    Console.WriteLine($"Conectado: ConnectionId={Context.ConnectionId}, UserId={userId}");
        //    return base.OnConnectedAsync();
        //}

        public async Task SendNotificationToUser(string userId, string title, string message, string actionURL)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                title,
                message,
                actionURL
            });
        }
    }
}
