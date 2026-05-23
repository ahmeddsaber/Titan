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
    public NotificationsController(INotificationService notificationService) { _notificationService = notificationService; }

    // FIX #3: Safe UserId extraction
    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.Identity?.Name, out userId);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int count = 20)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _notificationService.GetUserNotificationsAsync(userId, count));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _notificationService.GetUnreadCountAsync(userId));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _notificationService.MarkAsReadAsync(id, userId));
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _notificationService.MarkAllAsReadAsync(userId));
    }
}
