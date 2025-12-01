using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

[RequireRole(UserRole.Admin)]
public class UserManagementModel : PageModel
{
    private readonly ILogger<UserManagementModel> _logger;
    private readonly UserAdminService _userAdminService;

    public IReadOnlyCollection<AdminUserViewModel> Users { get; private set; } = Array.Empty<AdminUserViewModel>();
    public int CurrentPage { get; private set; } = 1;
    public int TotalPages { get; private set; } = 1;
    public int TotalCount { get; private set; }
    public int PageSize { get; private set; } = 20;

    public IReadOnlyCollection<SelectListItem> RoleOptions { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> StatusOptions { get; private set; } = Array.Empty<SelectListItem>();

    // Filter parameters
    [BindProperty(SupportsGet = true)]
    public string? Role { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public UserManagementModel(
        ILogger<UserManagementModel> logger,
        UserAdminService userAdminService)
    {
        _logger = logger;
        _userAdminService = userAdminService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        LoadFilterOptions();
        await LoadUsersAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed user management, found {UserCount} users",
            GetUserId(),
            TotalCount);

        return Page();
    }

    private void LoadFilterOptions()
    {
        RoleOptions = new List<SelectListItem>
        {
            new("All Roles", ""),
            new("Buyer", nameof(UserRole.Buyer)),
            new("Seller", nameof(UserRole.Seller)),
            new("Admin", nameof(UserRole.Admin)),
            new("Support", nameof(UserRole.Support)),
            new("Compliance", nameof(UserRole.Compliance))
        };

        StatusOptions = new List<SelectListItem>
        {
            new("All Statuses", ""),
            new("Active", nameof(UserStatus.Verified)),
            new("Pending Verification", nameof(UserStatus.Unverified)),
            new("Blocked", nameof(UserStatus.Suspended))
        };
    }

    private async Task LoadUsersAsync()
    {
        UserRole? roleFilter = null;
        if (!string.IsNullOrEmpty(Role) && Enum.TryParse<UserRole>(Role, out var parsedRole))
        {
            roleFilter = parsedRole;
        }

        UserStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(Status) && Enum.TryParse<UserStatus>(Status, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        var query = new GetUsersForAdminQuery(
            roleFilter,
            statusFilter,
            SearchTerm,
            Page,
            PageSize);

        var result = await _userAdminService.HandleAsync(query);

        Users = result.Items.Select(u => new AdminUserViewModel
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = u.CreatedAt,
            IsEmailVerified = u.IsEmailVerified
        }).ToList();

        CurrentPage = result.PageNumber;
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
