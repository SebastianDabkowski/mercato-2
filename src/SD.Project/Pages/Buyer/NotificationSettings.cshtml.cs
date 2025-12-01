using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page for managing notification settings including push notifications.
/// </summary>
[RequireRole(UserRole.Buyer, UserRole.Seller, UserRole.Admin)]
public class NotificationSettingsModel : PageModel
{
    private readonly ILogger<NotificationSettingsModel> _logger;
    private readonly PushSubscriptionService _pushSubscriptionService;

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public NotificationSettingsModel(
        ILogger<NotificationSettingsModel> logger,
        PushSubscriptionService pushSubscriptionService)
    {
        _logger = logger;
        _pushSubscriptionService = pushSubscriptionService;
    }

    public IActionResult OnGet()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out _))
        {
            return RedirectToPage("/Login");
        }

        if (!string.IsNullOrEmpty(StatusMessage))
        {
            SuccessMessage = StatusMessage;
        }

        return Page();
    }
}
