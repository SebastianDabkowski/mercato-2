using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Page model for the admin commission summary view.
/// </summary>
[RequireRole(UserRole.Admin)]
public class CommissionSummaryModel : PageModel
{
    private readonly ILogger<CommissionSummaryModel> _logger;
    private readonly CommissionSummaryService _commissionSummaryService;

    public CommissionSummaryModel(
        ILogger<CommissionSummaryModel> logger,
        CommissionSummaryService commissionSummaryService)
    {
        _logger = logger;
        _commissionSummaryService = commissionSummaryService;
    }

    public CommissionSummaryViewModel Summary { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? DateRange { get; set; } = "last30days";

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomFromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomToDate { get; set; }

    public IReadOnlyList<DateRangePreset> DateRangePresets { get; private set; } = Array.Empty<DateRangePreset>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        _logger.LogInformation("Admin commission summary accessed by user {UserId}",
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        // Set filter options
        DateRangePresets = new List<DateRangePreset>
        {
            new() { Name = "Last 7 Days", Value = "last7days", IsSelected = DateRange == "last7days" },
            new() { Name = "Last 30 Days", Value = "last30days", IsSelected = DateRange == "last30days" },
            new() { Name = "Last 90 Days", Value = "last90days", IsSelected = DateRange == "last90days" },
            new() { Name = "This Month", Value = "thismonth", IsSelected = DateRange == "thismonth" },
            new() { Name = "Last Month", Value = "lastmonth", IsSelected = DateRange == "lastmonth" },
            new() { Name = "Custom Range", Value = "custom", IsSelected = DateRange == "custom" }
        };

        // Calculate date range
        var (fromDate, toDate) = CalculateDateRange();

        // Get commission summary data
        var query = new GetCommissionSummaryQuery(fromDate, toDate);
        var result = await _commissionSummaryService.HandleAsync(query);

        // Map to view model
        Summary = new CommissionSummaryViewModel
        {
            Rows = result.Summaries.Select(s => new CommissionSummaryRowViewModel(
                s.StoreId,
                s.StoreName,
                s.OrderCount,
                s.TotalGmv,
                s.TotalCommission,
                s.TotalNetPayout,
                s.Currency)).ToList(),
            TotalGmv = result.TotalGmv,
            TotalCommission = result.TotalCommission,
            TotalNetPayout = result.TotalNetPayout,
            Currency = result.Currency,
            FromDate = result.FromDate,
            ToDate = result.ToDate
        };
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        _logger.LogInformation("Admin commission summary CSV export requested by user {UserId}",
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var (fromDate, toDate) = CalculateDateRange();

        var query = new ExportCommissionSummaryQuery(fromDate, toDate);
        var result = await _commissionSummaryService.HandleAsync(query);

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Export failed.";
            return RedirectToPage();
        }

        return File(result.FileData!, result.ContentType!, result.FileName);
    }

    private (DateTime FromDate, DateTime ToDate) CalculateDateRange()
    {
        var today = DateTime.UtcNow.Date;

        return DateRange switch
        {
            "last7days" => (today.AddDays(-6), today),
            "last30days" => (today.AddDays(-29), today),
            "last90days" => (today.AddDays(-89), today),
            "thismonth" => (new DateTime(today.Year, today.Month, 1), today),
            "lastmonth" => (new DateTime(today.Year, today.Month, 1).AddMonths(-1), 
                           new DateTime(today.Year, today.Month, 1).AddDays(-1)),
            "custom" => (
                CustomFromDate?.Date ?? today.AddDays(-29),
                CustomToDate?.Date ?? today
            ),
            _ => (today.AddDays(-29), today)
        };
    }
}
