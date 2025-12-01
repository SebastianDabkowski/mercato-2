using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Admin page for viewing user registration and activity analytics.
/// All metrics are aggregated and anonymised for privacy compliance.
/// </summary>
[RequireRole(UserRole.Admin)]
public class UserAnalyticsModel : PageModel
{
    private readonly ILogger<UserAnalyticsModel> _logger;
    private readonly UserAnalyticsService _userAnalyticsService;

    public UserAnalyticsModel(ILogger<UserAnalyticsModel> logger, UserAnalyticsService userAnalyticsService)
    {
        _logger = logger;
        _userAnalyticsService = userAnalyticsService;
    }

    public UserAnalyticsViewModel Analytics { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? DateRange { get; set; } = "last7days";

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomFromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomToDate { get; set; }

    public IReadOnlyList<DateRangePreset> DateRangePresets { get; private set; } = Array.Empty<DateRangePreset>();

    public async Task OnGetAsync()
    {
        _logger.LogInformation("User analytics page accessed by user {UserId}",
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

        // Set date range presets
        DateRangePresets = new List<DateRangePreset>
        {
            new() { Name = "Today", Value = "today", IsSelected = DateRange == "today" },
            new() { Name = "Last 7 Days", Value = "last7days", IsSelected = DateRange == "last7days" },
            new() { Name = "Last 30 Days", Value = "last30days", IsSelected = DateRange == "last30days" },
            new() { Name = "Custom Range", Value = "custom", IsSelected = DateRange == "custom" }
        };

        // Calculate date range
        var (fromDate, toDate) = CalculateDateRange();

        // Get user analytics metrics
        var query = new GetUserAnalyticsQuery(fromDate, toDate);
        var metrics = await _userAnalyticsService.HandleAsync(query);

        // Map to view model
        Analytics = new UserAnalyticsViewModel
        {
            NewBuyerCount = metrics.NewBuyerCount,
            NewSellerCount = metrics.NewSellerCount,
            ActiveUserCount = metrics.ActiveUserCount,
            UsersWithOrdersCount = metrics.UsersWithOrdersCount,
            FromDate = metrics.FromDate,
            ToDate = metrics.ToDate,
            HasData = metrics.HasData,
            RefreshedAt = metrics.RefreshedAt
        };
    }

    private (DateTime FromDate, DateTime ToDate) CalculateDateRange()
    {
        var today = DateTime.UtcNow.Date;

        return DateRange switch
        {
            "today" => (today, today),
            "last7days" => (today.AddDays(-6), today),
            "last30days" => (today.AddDays(-29), today),
            "custom" => (
                CustomFromDate?.Date ?? today.AddDays(-6),
                CustomToDate?.Date ?? today
            ),
            _ => (today.AddDays(-6), today)
        };
    }
}
