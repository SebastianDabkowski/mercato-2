using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

[RequireRole(UserRole.Admin)]
public class UserDetailsModel : PageModel
{
    private readonly ILogger<UserDetailsModel> _logger;
    private readonly UserAdminService _userAdminService;

    public AdminUserDetailViewModel? UserDetail { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public UserDetailsModel(
        ILogger<UserDetailsModel> logger,
        UserAdminService userAdminService)
    {
        _logger = logger;
        _userAdminService = userAdminService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id == Guid.Empty)
        {
            ErrorMessage = "Invalid user ID.";
            return Page();
        }

        var query = new GetUserDetailQuery(Id);
        var userDetail = await _userAdminService.HandleAsync(query);

        if (userDetail is null)
        {
            ErrorMessage = "User not found.";
            _logger.LogWarning("Admin {AdminId} attempted to view non-existent user {UserId}",
                GetUserId(),
                Id);
            return Page();
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

        _logger.LogInformation("Admin {AdminId} viewed user details for {UserId}",
            GetUserId(),
            Id);

        return Page();
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
