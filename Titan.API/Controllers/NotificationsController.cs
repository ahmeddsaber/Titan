using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Titan.Application.Interfaces;

namespace Titan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public NotificationsController(INotificationService notificationService) { _notificationService = notificationService; }

    [HttpGet] public async Task<IActionResult> Get([FromQuery] int count = 20) => Ok(await _notificationService.GetUserNotificationsAsync(UserId, count));
    [HttpGet("unread-count")] public async Task<IActionResult> UnreadCount() => Ok(await _notificationService.GetUnreadCountAsync(UserId));
    [HttpPut("{id:guid}/read")] public async Task<IActionResult> MarkRead(Guid id) => Ok(await _notificationService.MarkAsReadAsync(id, UserId));
    [HttpPut("mark-all-read")] public async Task<IActionResult> MarkAllRead() => Ok(await _notificationService.MarkAllAsReadAsync(UserId));
}
