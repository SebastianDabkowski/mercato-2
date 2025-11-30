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
/// Page model for displaying seller's payout history with filtering.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class PayoutsModel : PageModel
{
    private readonly ILogger<PayoutsModel> _logger;
    private readonly PayoutScheduleService _payoutScheduleService;
    private readonly StoreService _storeService;

    public IReadOnlyList<PayoutHistoryListItemViewModel> Payouts { get; private set; } = [];
    public string? StoreName { get; private set; }
    public Guid? StoreId { get; private set; }

    // Pagination
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    // Pagination helper properties for display
    public int DisplayStartItem => TotalCount > 0 ? (CurrentPage - 1) * PageSize + 1 : 0;
    public int DisplayEndItem => Math.Min(CurrentPage * PageSize, TotalCount);

    // Available statuses for filtering
    public IReadOnlyList<string> AvailableStatuses { get; } = new[]
    {
        "Scheduled", "Processing", "Paid", "Failed"
    };

    // Filter properties bound from query string
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PayoutsModel(
        ILogger<PayoutsModel> logger,
        PayoutScheduleService payoutScheduleService,
        StoreService storeService)
    {
        _logger = logger;
        _payoutScheduleService = payoutScheduleService;
        _storeService = storeService;
    }

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
        CurrentPage = Math.Max(1, PageNumber);

        // Get filtered payout history
        var result = await _payoutScheduleService.HandleAsync(
            new GetPayoutHistoryQuery(
                store.Id,
                Status,
                FromDate,
                ToDate,
                CurrentPage,
                PageSize),
            cancellationToken);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;

        Payouts = result.Items.Select(p => new PayoutHistoryListItemViewModel(
            p.Id,
            p.TotalAmount,
            p.Currency,
            p.Status,
            p.ScheduledDate,
            p.PayoutMethod,
            p.ItemCount,
            p.PaidAt,
            p.FailedAt,
            p.ErrorMessage)).ToList().AsReadOnly();

        _logger.LogInformation("Payout history page accessed for store {StoreId} with {PayoutCount} payouts (filtered: status={Status}, fromDate={FromDate}, toDate={ToDate})",
            store.Id, Payouts.Count, Status, FromDate, ToDate);

        return Page();
    }
}
