using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller;

/// <summary>
/// Page model for the seller order and revenue report.
/// Displays order data with financial fields for sales reconciliation.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class OrderReportModel : PageModel
{
    private readonly ILogger<OrderReportModel> _logger;
    private readonly SellerOrderReportService _reportService;
    private readonly StoreService _storeService;

    public OrderReportModel(
        ILogger<OrderReportModel> logger,
        SellerOrderReportService reportService,
        StoreService storeService)
    {
        _logger = logger;
        _reportService = reportService;
        _storeService = storeService;
    }

    public SellerOrderReportViewModel Report { get; private set; } = new();
    public string? StoreName { get; private set; }
    public Guid? StoreId { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? DateRange { get; set; } = "last30days";

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomFromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? OrderStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<DateRangePreset> DateRangePresets { get; private set; } = Array.Empty<DateRangePreset>();
    public IReadOnlyList<string> OrderStatusOptions { get; private set; } = Array.Empty<string>();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        // Get seller's store
        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Store not found for seller {SellerId}", userId);
            return Page();
        }

        StoreId = store.Id;
        StoreName = store.Name;

        _logger.LogInformation("Seller order report accessed by user {UserId} for store {StoreId}",
            userId, store.Id);

        // Set filter options
        DateRangePresets = new List<DateRangePreset>
        {
            new() { Name = "Last 7 Days", Value = "last7days", IsSelected = DateRange == "last7days" },
            new() { Name = "Last 30 Days", Value = "last30days", IsSelected = DateRange == "last30days" },
            new() { Name = "Last 90 Days", Value = "last90days", IsSelected = DateRange == "last90days" },
            new() { Name = "Custom Range", Value = "custom", IsSelected = DateRange == "custom" }
        };

        OrderStatusOptions = new List<string>
        {
            "Pending", "Paid", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded"
        };

        // Calculate date range
        var (fromDate, toDate) = CalculateDateRange();

        // Get report data
        var query = new GetSellerOrderReportQuery(
            store.Id,
            fromDate,
            toDate,
            OrderStatus,
            PageNumber,
            20);

        var result = await _reportService.HandleAsync(query, cancellationToken);

        // Map to view model
        Report = new SellerOrderReportViewModel
        {
            Rows = result.Rows.Select(r => new SellerOrderReportRowViewModel
            {
                OrderId = r.OrderId,
                SubOrderId = r.SubOrderId,
                OrderNumber = r.OrderNumber,
                OrderDate = r.OrderDate,
                BuyerName = r.BuyerName,
                OrderStatus = r.OrderStatus,
                PaymentStatus = r.PaymentStatus,
                OrderValue = r.OrderValue,
                Commission = r.Commission,
                NetAmount = r.NetAmount,
                Currency = r.Currency
            }).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalOrderValue = result.TotalOrderValue,
            TotalCommission = result.TotalCommission,
            TotalNetAmount = result.TotalNetAmount,
            Currency = result.Currency,
            ExceedsExportThreshold = result.ExceedsExportThreshold
        };

        return Page();
    }

    public async Task<IActionResult> OnGetExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        // Get seller's store
        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Store not found for seller {SellerId} during export", userId);
            return RedirectToPage();
        }

        _logger.LogInformation("Seller CSV export requested by user {UserId} for store {StoreId}",
            userId, store.Id);

        var (fromDate, toDate) = CalculateDateRange();

        var query = new ExportSellerOrderReportQuery(
            store.Id,
            fromDate,
            toDate,
            OrderStatus);

        var result = await _reportService.HandleAsync(query, cancellationToken);

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
            "custom" => (
                CustomFromDate?.Date ?? today.AddDays(-29),
                CustomToDate?.Date ?? today
            ),
            _ => (today.AddDays(-29), today)
        };
    }
}
