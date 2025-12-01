using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

[RequireRole(UserRole.Admin, UserRole.Support, UserRole.Compliance)]
public class UserDetailsModel : PageModel
{
    private readonly ILogger<UserDetailsModel> _logger;
    private readonly UserAdminService _userAdminService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IAuthorizationService _authorizationService;

    public AdminUserDetailViewModel? UserDetail { get; private set; }
    public IReadOnlyList<UserBlockHistoryViewModel> BlockHistory { get; private set; } = Array.Empty<UserBlockHistoryViewModel>();
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public string? ReactivationNotes { get; set; }

    public UserDetailsModel(
        ILogger<UserDetailsModel> logger,
        UserAdminService userAdminService,
        IAuditLoggingService auditLoggingService,
        IAuthorizationService authorizationService)
    {
        _logger = logger;
        _userAdminService = userAdminService;
        _auditLoggingService = auditLoggingService;
        _authorizationService = authorizationService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id == Guid.Empty)
        {
            ErrorMessage = "Invalid user ID.";
            return Page();
        }

        await LoadUserDetailsAsync();

        if (UserDetail is null)
        {
            ErrorMessage = "User not found.";
            _logger.LogWarning("Admin {AdminId} attempted to view non-existent user {UserId}",
                GetAdminId(),
                Id);
        }
        else
        {
            _logger.LogInformation("Admin {AdminId} viewed user details for {UserId}",
                GetAdminId(),
                Id);

            // Log sensitive data access for audit compliance
            await LogSensitiveAccessAsync(SensitiveAccessAction.View);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReactivateAsync()
    {
        if (Id == Guid.Empty)
        {
            ErrorMessage = "Invalid user ID.";
            await LoadUserDetailsAsync();
            return Page();
        }

        var adminId = GetAdminIdGuid();
        if (adminId == Guid.Empty)
        {
            ErrorMessage = "Unable to identify admin user.";
            await LoadUserDetailsAsync();
            return Page();
        }

        var command = new ReactivateUserCommand(Id, adminId, ReactivationNotes);
        var result = await _userAdminService.HandleAsync(command);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
        }
        else
        {
            SuccessMessage = "User account has been successfully reactivated.";
            _logger.LogInformation("Admin {AdminId} reactivated user {UserId}",
                adminId,
                Id);

            // Log sensitive data modification for audit compliance
            await LogSensitiveAccessAsync(SensitiveAccessAction.Modify);
        }

        await LoadUserDetailsAsync();
        return Page();
    }

    private async Task LogSensitiveAccessAsync(SensitiveAccessAction action)
    {
        var adminId = GetAdminIdGuid();
        var userRole = GetUserRole();

        // Check if audit logging is required for this user role accessing customer profile
        if (_authorizationService.RequiresAuditLogging(userRole, SensitiveResourceType.CustomerProfile))
        {
            await _auditLoggingService.LogSensitiveAccessAsync(
                adminId,
                userRole,
                SensitiveResourceType.CustomerProfile,
                Id,
                action,
                Id, // Resource owner is the user being viewed
                null, // No specific reason provided
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }
    }

    private async Task LoadUserDetailsAsync()
    {
        var query = new GetUserDetailQuery(Id);
        var userDetail = await _userAdminService.HandleAsync(query);

        if (userDetail is null)
        {
            return;
        }

        UserDetail = new AdminUserDetailViewModel
        {
            Id = userDetail.Id,
            Email = userDetail.Email,
            FirstName = userDetail.FirstName,
            LastName = userDetail.LastName,
            Role = userDetail.Role,
            Status = userDetail.Status,
            CreatedAt = userDetail.CreatedAt,
            IsEmailVerified = userDetail.IsEmailVerified,
            EmailVerifiedAt = userDetail.EmailVerifiedAt,
            CompanyName = userDetail.CompanyName,
            PhoneNumber = userDetail.PhoneNumber,
            KycStatus = userDetail.KycStatus,
            KycSubmittedAt = userDetail.KycSubmittedAt,
            KycReviewedAt = userDetail.KycReviewedAt,
            TwoFactorEnabled = userDetail.TwoFactorEnabled,
            TwoFactorEnabledAt = userDetail.TwoFactorEnabledAt,
            RecentLoginActivity = userDetail.RecentLoginActivity.Select(e => new LoginEventViewModel
            {
                OccurredAt = e.OccurredAt,
                IsSuccess = e.IsSuccess,
                EventType = e.EventType,
                IpAddress = e.IpAddress,
                Location = e.Location,
                FailureReason = e.FailureReason
            }).ToList()
        };

        // Load block history
        var historyQuery = new GetUserBlockHistoryQuery(Id);
        var history = await _userAdminService.HandleAsync(historyQuery);

        BlockHistory = history.Select(h => new UserBlockHistoryViewModel
        {
            Id = h.Id,
            BlockedByAdminId = h.BlockedByAdminId,
            BlockedByAdminName = h.BlockedByAdminName,
            BlockedAt = h.BlockedAt,
            Reason = h.Reason,
            BlockNotes = h.BlockNotes,
            IsActive = h.IsActive,
            ReactivatedByAdminId = h.ReactivatedByAdminId,
            ReactivatedByAdminName = h.ReactivatedByAdminName,
            ReactivatedAt = h.ReactivatedAt,
            ReactivationNotes = h.ReactivationNotes
        }).ToList();
    }

    private string GetAdminId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }

    private Guid GetAdminIdGuid()
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(adminIdClaim, out var adminId) ? adminId : Guid.Empty;
    }

    private UserRole GetUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.Buyer;
    }
}
