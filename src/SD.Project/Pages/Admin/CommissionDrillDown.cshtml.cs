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
/// Page model for the commission drill-down view showing individual orders for a seller.
/// </summary>
[RequireRole(UserRole.Admin)]
public class CommissionDrillDownModel : PageModel
{
    private readonly ILogger<CommissionDrillDownModel> _logger;
    private readonly CommissionSummaryService _commissionSummaryService;

    public CommissionDrillDownModel(
        ILogger<CommissionDrillDownModel> logger,
        CommissionSummaryService commissionSummaryService)
    {
        _logger = logger;
        _commissionSummaryService = commissionSummaryService;
    }

    public CommissionDrillDownViewModel DrillDown { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public Guid StoreId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime ToDate { get; set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (StoreId == Guid.Empty)
        {
            return RedirectToPage("/Admin/CommissionSummary", new { error = "Store ID is required." });
        }

        _logger.LogInformation("Admin commission drill-down accessed for store {StoreId} by user {UserId}",
            StoreId,
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        // Default date range if not specified
        if (FromDate == default)
        {
            FromDate = DateTime.UtcNow.Date.AddDays(-29);
        }
        if (ToDate == default)
        {
            ToDate = DateTime.UtcNow.Date;
        }

        // Get drill-down data
        var query = new GetCommissionDrillDownQuery(StoreId, FromDate, ToDate);
        var result = await _commissionSummaryService.HandleAsync(query);

        if (result is null)
        {
            return RedirectToPage("/Admin/CommissionSummary", new { error = "Store not found." });
        }

        // Map to view model
        DrillDown = new CommissionDrillDownViewModel
        {
            StoreId = result.StoreId,
            StoreName = result.StoreName,
            Orders = result.Orders.Select(o => new CommissionOrderDetailViewModel(
                o.AllocationId,
                o.ShipmentId,
                o.OrderNumber,
                o.OrderDate,
                o.GmvAmount,
                o.CommissionAmount,
                o.CommissionRate,
                o.NetPayout,
                o.RefundedAmount,
                o.Currency)).ToList(),
            TotalGmv = result.TotalGmv,
            TotalCommission = result.TotalCommission,
            TotalNetPayout = result.TotalNetPayout,
            Currency = result.Currency,
            FromDate = result.FromDate,
            ToDate = result.ToDate
        };

        return Page();
    }
}
