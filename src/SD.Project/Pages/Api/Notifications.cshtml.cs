using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;

namespace SD.Project.Pages.Api;

/// <summary>
/// API endpoint for notification operations.
/// </summary>
public class NotificationsModel : PageModel
{
    private readonly NotificationCenterService _notificationService;

    public NotificationsModel(NotificationCenterService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets the unread notification count for the current user.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new JsonResult(new { count = 0 });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new JsonResult(new { count = 0 });
        }

        var result = await _notificationService.HandleAsync(
            new GetUnreadNotificationCountQuery(userId),
            cancellationToken);

        return new JsonResult(new { count = result.Count });
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    public async Task<IActionResult> OnPostMarkAsReadAsync(
        [FromBody] MarkAsReadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedResult();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new UnauthorizedResult();
        }

        var command = new MarkNotificationAsReadCommand(request.NotificationId, userId);
        var result = await _notificationService.HandleAsync(command, cancellationToken);

        return new JsonResult(new { success = result.Success, markedCount = result.MarkedCount });
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    public async Task<IActionResult> OnPostMarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedResult();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new UnauthorizedResult();
        }

        var command = new MarkAllNotificationsAsReadCommand(userId);
        var result = await _notificationService.HandleAsync(command, cancellationToken);

        return new JsonResult(new { success = result.Success, markedCount = result.MarkedCount });
    }

    public record MarkAsReadRequest(Guid NotificationId);
}
