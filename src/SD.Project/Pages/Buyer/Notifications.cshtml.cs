using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Notification center page for managing user notifications.
/// </summary>
[RequireRole(UserRole.Buyer, UserRole.Seller, UserRole.Admin)]
public class NotificationsModel : PageModel
{
    private readonly ILogger<NotificationsModel> _logger;
    private readonly NotificationCenterService _notificationService;

    public NotificationCenterViewModel Notifications { get; private set; } = new();
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public NotificationsModel(
        ILogger<NotificationsModel> logger,
        NotificationCenterService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> OnGetAsync(
        string? filter = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        _logger.LogDebug("Loading notifications for user {UserId}, filter: {Filter}, page: {Page}", 
            userId, filter, page);

        // Parse filter
        bool? isReadFilter = filter switch
        {
            "unread" => false,
            "read" => true,
            _ => null
        };

        var query = new GetNotificationsQuery(userId, isReadFilter, null, page, 20);
        var result = await _notificationService.HandleAsync(query, cancellationToken);

        Notifications = new NotificationCenterViewModel
        {
            Notifications = result.Items.Select(n => new NotificationViewModel(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.RelatedUrl,
                n.IsRead,
                n.CreatedAt,
                FormatTimeAgo(n.CreatedAt)
            )).ToList(),
            TotalCount = result.TotalCount,
            UnreadCount = result.UnreadCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            FilterType = filter
        };

        if (!string.IsNullOrEmpty(StatusMessage))
        {
            SuccessMessage = StatusMessage;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var command = new MarkNotificationAsReadCommand(notificationId, userId);
        var result = await _notificationService.HandleAsync(command, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", 
                notificationId, userId);
        }

        return RedirectToPage(new { filter = Request.Query["filter"].ToString() });
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var command = new MarkAllNotificationsAsReadCommand(userId);
        var result = await _notificationService.HandleAsync(command, cancellationToken);

        if (result.Success)
        {
            StatusMessage = $"Marked {result.MarkedCount} notifications as read.";
            _logger.LogInformation("User {UserId} marked {Count} notifications as read", 
                userId, result.MarkedCount);
        }

        return RedirectToPage(new { filter = Request.Query["filter"].ToString() });
    }

    private static string FormatTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}w ago";
        
        return dateTime.ToString("MMM dd, yyyy");
    }
}
