using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for submitting a seller rating after order delivery.
/// </summary>
public class SubmitSellerRatingModel : PageModel
{
    private readonly ILogger<SubmitSellerRatingModel> _logger;
    private readonly OrderService _orderService;
    private readonly SellerRatingService _sellerRatingService;

    /// <summary>
    /// Order details for context.
    /// </summary>
    public OrderDetailsViewModel? Order { get; private set; }

    /// <summary>
    /// Store name for the seller being rated.
    /// </summary>
    public string? StoreName { get; private set; }

    /// <summary>
    /// Seller rating eligibility status.
    /// </summary>
    public SellerRatingEligibilityViewModel? Eligibility { get; private set; }

    [BindProperty]
    public Guid OrderId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please select a rating")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [BindProperty]
    [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")]
    public string? Comment { get; set; }

    public SubmitSellerRatingModel(
        ILogger<SubmitSellerRatingModel> logger,
        OrderService orderService,
        SellerRatingService sellerRatingService)
    {
        _logger = logger;
        _orderService = orderService;
        _sellerRatingService = sellerRatingService;
    }

    /// <summary>
    /// Loads page data without modifying bound properties.
    /// </summary>
    private async Task<bool> LoadPageDataAsync(
        Guid buyerId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        // Check eligibility
        var eligibility = await _sellerRatingService.HandleAsync(
            new CheckSellerRatingEligibilityQuery(buyerId, orderId),
            cancellationToken);

        Eligibility = new SellerRatingEligibilityViewModel(
            eligibility.IsEligible,
            eligibility.IneligibilityReason,
            eligibility.HasExistingRating);

        // Get order details
        var orderDetails = await _orderService.HandleAsync(
            new GetBuyerOrderDetailsQuery(buyerId, orderId),
            cancellationToken);

        if (orderDetails is null)
        {
            return false;
        }

        // Get primary store name
        var primarySubOrder = orderDetails.SellerSubOrders.FirstOrDefault();
        StoreName = primarySubOrder?.StoreName ?? "Seller";

        // Map to ViewModel
        Order = new OrderDetailsViewModel(
            orderDetails.OrderId,
            orderDetails.OrderNumber,
            orderDetails.Status,
            orderDetails.PaymentStatus,
            orderDetails.PaymentStatusMessage,
            orderDetails.RecipientName,
            orderDetails.DeliveryAddressSummary,
            orderDetails.PaymentMethodName,
            orderDetails.ItemSubtotal,
            orderDetails.TotalShipping,
            orderDetails.TotalAmount,
            orderDetails.Currency,
            orderDetails.CreatedAt,
            orderDetails.EstimatedDeliveryRange,
            orderDetails.SellerSubOrders.Select(s => new SellerSubOrderViewModel(
                s.SubOrderId,
                s.StoreId,
                s.StoreName,
                s.Status,
                s.Subtotal,
                s.ShippingCost,
                s.Total,
                s.Items.Select(i => new OrderItemViewModel(
                    i.ProductId,
                    i.ProductName,
                    i.StoreId,
                    i.StoreName,
                    i.UnitPrice,
                    i.Quantity,
                    i.LineTotal,
                    i.ShippingMethodName,
                    i.ShippingCost,
                    i.EstimatedDelivery)).ToList().AsReadOnly(),
                s.CarrierName,
                s.TrackingNumber,
                s.TrackingUrl,
                s.ShippedAt,
                s.DeliveredAt,
                s.EstimatedDelivery,
                s.CancelledAt,
                s.RefundedAt,
                s.RefundedAmount)).ToList().AsReadOnly(),
            orderDetails.CancelledAt,
            orderDetails.RefundedAt,
            orderDetails.RefundedAmount);

        return true;
    }

    public async Task<IActionResult> OnGetAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Login", new { returnUrl = $"/Buyer/SubmitSellerRating?orderId={orderId}" });
        }

        var buyerId = GetBuyerId();
        if (!buyerId.HasValue)
        {
            return RedirectToPage("/Login");
        }

        // Check eligibility first
        var eligibility = await _sellerRatingService.HandleAsync(
            new CheckSellerRatingEligibilityQuery(buyerId.Value, orderId),
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            TempData["Error"] = eligibility.IneligibilityReason ?? "You cannot rate this seller.";
            return RedirectToPage("/Buyer/OrderDetails", new { orderId });
        }

        // Load page data
        if (!await LoadPageDataAsync(buyerId.Value, orderId, cancellationToken))
        {
            return RedirectToPage("/Buyer/Orders");
        }

        // Set hidden form field
        OrderId = orderId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Login");
        }

        var buyerId = GetBuyerId();
        if (!buyerId.HasValue)
        {
            return RedirectToPage("/Login");
        }

        if (!ModelState.IsValid)
        {
            // Reload page data while preserving bound property values
            await LoadPageDataAsync(buyerId.Value, OrderId, cancellationToken);
            return Page();
        }

        // Submit the seller rating
        var result = await _sellerRatingService.HandleAsync(
            new SubmitSellerRatingCommand(
                buyerId.Value,
                OrderId,
                Rating,
                Comment),
            cancellationToken);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to submit rating.";
            // Reload page data while preserving bound property values
            await LoadPageDataAsync(buyerId.Value, OrderId, cancellationToken);
            return Page();
        }

        _logger.LogInformation(
            "Seller rating submitted for order {OrderId} by buyer {BuyerId}",
            OrderId, buyerId);

        TempData["Success"] = "Thank you for rating the seller!";
        return RedirectToPage("/Buyer/OrderDetails", new { orderId = OrderId });
    }

    private Guid? GetBuyerId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var buyerId))
        {
            return buyerId;
        }
        return null;
    }
}
