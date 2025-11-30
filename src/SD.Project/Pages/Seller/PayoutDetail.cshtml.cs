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
/// Page model for displaying payout details with order breakdown.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class PayoutDetailModel : PageModel
{
    private readonly ILogger<PayoutDetailModel> _logger;
    private readonly PayoutScheduleService _payoutScheduleService;
    private readonly StoreService _storeService;

    public PayoutHistoryDetailsViewModel? Payout { get; private set; }
    public string? StoreName { get; private set; }
    public Guid? StoreId { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid PayoutId { get; set; }

    public PayoutDetailModel(
        ILogger<PayoutDetailModel> logger,
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
            return RedirectToPage("/Seller/Payouts");
        }

        StoreId = store.Id;
        StoreName = store.Name;

        // Get payout details
        var payoutDetails = await _payoutScheduleService.HandleAsync(
            new GetPayoutDetailsQuery(PayoutId, store.Id),
            cancellationToken);

        if (payoutDetails is null)
        {
            _logger.LogWarning("Payout {PayoutId} not found or does not belong to store {StoreId}", PayoutId, store.Id);
            return RedirectToPage("/Seller/Payouts");
        }

        // Map to view model
        Payout = new PayoutHistoryDetailsViewModel(
            payoutDetails.Id,
            payoutDetails.StoreId,
            payoutDetails.TotalAmount,
            payoutDetails.Currency,
            payoutDetails.Status,
            payoutDetails.ScheduledDate,
            payoutDetails.PayoutMethod,
            payoutDetails.PayoutReference,
            payoutDetails.ErrorReference,
            payoutDetails.ErrorMessage,
            payoutDetails.RetryCount,
            payoutDetails.MaxRetries,
            payoutDetails.CanRetry,
            payoutDetails.OrderBreakdown.Select(o => new PayoutOrderBreakdownViewModel(
                o.EscrowAllocationId,
                o.ShipmentId,
                o.OrderNumber,
                o.SellerAmount,
                o.ShippingAmount,
                o.CommissionAmount,
                o.PayoutAmount,
                o.CreatedAt)).ToList().AsReadOnly(),
            payoutDetails.CreatedAt,
            payoutDetails.ProcessedAt,
            payoutDetails.PaidAt,
            payoutDetails.FailedAt,
            payoutDetails.NextRetryAt);

        _logger.LogInformation("Payout detail page accessed for payout {PayoutId} in store {StoreId}",
            PayoutId, store.Id);

        return Page();
    }
}
