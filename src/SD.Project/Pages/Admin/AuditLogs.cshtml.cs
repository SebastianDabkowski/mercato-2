using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SD.Project.Application.DTOs;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Admin page for viewing and filtering audit logs of administrative actions.
/// Access is restricted to Admin, Compliance, and Support roles.
/// </summary>
[RequireRole(UserRole.Admin, UserRole.Compliance, UserRole.Support)]
public class AuditLogsModel : PageModel
{
    private readonly ILogger<AuditLogsModel> _logger;
    private readonly CriticalActionAuditQueryService _auditQueryService;

    /// <summary>
    /// List of audit log entries to display.
    /// </summary>
    public IReadOnlyCollection<AuditLogViewModel> AuditLogs { get; private set; } = Array.Empty<AuditLogViewModel>();

    /// <summary>
    /// Current page number for pagination.
    /// </summary>
    public int CurrentPage { get; private set; } = 1;

    /// <summary>
    /// Total number of pages available.
    /// </summary>
    public int TotalPages { get; private set; } = 1;

    /// <summary>
    /// Total count of matching audit log entries.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; private set; } = 25;

    /// <summary>
    /// Dropdown options for action type filter.
    /// </summary>
    public IReadOnlyCollection<SelectListItem> ActionTypeOptions { get; private set; } = Array.Empty<SelectListItem>();

    /// <summary>
    /// Dropdown options for outcome filter.
    /// </summary>
    public IReadOnlyCollection<SelectListItem> OutcomeOptions { get; private set; } = Array.Empty<SelectListItem>();

    /// <summary>
    /// Start date filter (inclusive).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date filter (inclusive).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by admin user ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? AdminUserId { get; set; }

    /// <summary>
    /// Filter by action type.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ActionType { get; set; }

    /// <summary>
    /// Filter by outcome.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Outcome { get; set; }

    /// <summary>
    /// Filter by target resource type.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ResourceType { get; set; }

    /// <summary>
    /// Filter by target resource ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ResourceId { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    /// <summary>
    /// Indicates if authorization was denied.
    /// </summary>
    public bool IsAccessDenied { get; private set; }

    public AuditLogsModel(
        ILogger<AuditLogsModel> logger,
        CriticalActionAuditQueryService auditQueryService)
    {
        _logger = logger;
        _auditQueryService = auditQueryService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        LoadFilterOptions();

        // Set default date range if not provided (last 30 days)
        if (!FromDate.HasValue)
        {
            FromDate = DateTime.UtcNow.AddDays(-30).Date;
        }
        if (!ToDate.HasValue)
        {
            ToDate = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1); // End of today
        }

        await LoadAuditLogsAsync();

        if (IsAccessDenied)
        {
            _logger.LogWarning(
                "User {UserId} was denied access to audit logs",
                GetUserId());
            return RedirectToPage("/Error", new { message = "Access denied. You are not authorized to view audit logs." });
        }

        _logger.LogInformation(
            "User {UserId} viewed audit logs, found {LogCount} entries",
            GetUserId(),
            TotalCount);

        return Page();
    }

    private void LoadFilterOptions()
    {
        ActionTypeOptions = new List<SelectListItem>
        {
            new("All Action Types", ""),
            new("Login", nameof(CriticalActionType.Login)),
            new("Logout", nameof(CriticalActionType.Logout)),
            new("Role Change", nameof(CriticalActionType.RoleChange)),
            new("Payout Change", nameof(CriticalActionType.PayoutChange)),
            new("Order Status Override", nameof(CriticalActionType.OrderStatusOverride)),
            new("Refund", nameof(CriticalActionType.Refund)),
            new("Account Deletion", nameof(CriticalActionType.AccountDeletion)),
            new("Password Change", nameof(CriticalActionType.PasswordChange)),
            new("2FA Change", nameof(CriticalActionType.TwoFactorChange)),
            new("Permission Change", nameof(CriticalActionType.PermissionChange)),
            new("Settlement Adjustment", nameof(CriticalActionType.SettlementAdjustment)),
            new("Data Export", nameof(CriticalActionType.DataExport)),
            new("User Block/Unblock", nameof(CriticalActionType.UserBlock)),
            new("Store Status Change", nameof(CriticalActionType.StoreStatusChange))
        };

        OutcomeOptions = new List<SelectListItem>
        {
            new("All Outcomes", ""),
            new("Success", nameof(CriticalActionOutcome.Success)),
            new("Failure", nameof(CriticalActionOutcome.Failure))
        };
    }

    private async Task LoadAuditLogsAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
        {
            IsAccessDenied = true;
            return;
        }

        if (string.IsNullOrEmpty(roleClaim) || !Enum.TryParse<UserRole>(roleClaim, out var requestingUserRole))
        {
            IsAccessDenied = true;
            return;
        }

        // Parse filters
        Guid? adminUserIdFilter = null;
        if (!string.IsNullOrEmpty(AdminUserId) && Guid.TryParse(AdminUserId, out var parsedAdminUserId))
        {
            adminUserIdFilter = parsedAdminUserId;
        }

        CriticalActionType? actionTypeFilter = null;
        if (!string.IsNullOrEmpty(ActionType) && Enum.TryParse<CriticalActionType>(ActionType, out var parsedActionType))
        {
            actionTypeFilter = parsedActionType;
        }

        CriticalActionOutcome? outcomeFilter = null;
        if (!string.IsNullOrEmpty(Outcome) && Enum.TryParse<CriticalActionOutcome>(Outcome, out var parsedOutcome))
        {
            outcomeFilter = parsedOutcome;
        }

        // If filtering by specific resource
        if (!string.IsNullOrEmpty(ResourceType) && !string.IsNullOrEmpty(ResourceId) && Guid.TryParse(ResourceId, out var resourceGuid))
        {
            var resourceLogs = await _auditQueryService.GetAuditLogsByResourceAsync(
                requestingUserId,
                requestingUserRole,
                ResourceType,
                resourceGuid,
                (Page - 1) * PageSize,
                PageSize);

            if (resourceLogs == null)
            {
                IsAccessDenied = true;
                return;
            }

            AuditLogs = resourceLogs.Select(MapToViewModel).ToList();
            TotalCount = resourceLogs.Count;
            TotalPages = 1; // Resource-filtered results typically don't paginate
            CurrentPage = 1;
            return;
        }

        // General query with filters
        var query = new CriticalActionAuditLogQueryDto
        {
            FromDate = FromDate!.Value,
            ToDate = ToDate!.Value,
            UserId = adminUserIdFilter,
            ActionType = actionTypeFilter,
            Outcome = outcomeFilter,
            Skip = (Page - 1) * PageSize,
            Take = PageSize
        };

        var result = await _auditQueryService.QueryAuditLogsAsync(
            requestingUserId,
            requestingUserRole,
            query);

        if (result == null)
        {
            IsAccessDenied = true;
            return;
        }

        AuditLogs = result.Items.Select(MapToViewModel).ToList();
        TotalCount = result.TotalCount;
        TotalPages = (int)Math.Ceiling((double)result.TotalCount / PageSize);
        CurrentPage = Page;
    }

    private static AuditLogViewModel MapToViewModel(CriticalActionAuditLogDto dto)
    {
        return new AuditLogViewModel
        {
            Id = dto.Id,
            UserId = dto.UserId,
            UserEmail = null, // Could be populated with a user lookup if needed
            UserRole = dto.UserRole,
            ActionType = dto.ActionType,
            TargetResourceType = dto.TargetResourceType,
            TargetResourceId = dto.TargetResourceId,
            Outcome = dto.Outcome,
            Details = dto.Details,
            IpAddress = dto.IpAddress,
            OccurredAt = dto.OccurredAt
        };
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
