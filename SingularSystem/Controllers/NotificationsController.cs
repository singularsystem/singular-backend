using SingularSystem.Models;
using SingularSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SingularSystem.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(NotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetNotifications(Guid userId)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsFromUser(userId);
                return Ok(new ApiResponse<object>()
                {
                    Success = true,
                    Message = "Notificações retornadas com sucesso",
                    Data = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        [Authorize]
        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(Guid notificationId)
        {
            try
            {
                await _notificationService.MarkAsRead(notificationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        [Authorize]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                {
                    return Unauthorized();
                }

                await _notificationService.MarkAllAsRead(userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }
    }
}
