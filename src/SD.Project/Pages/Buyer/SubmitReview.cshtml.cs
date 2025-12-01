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
/// Page model for submitting a product review after order delivery.
/// </summary>
public class SubmitReviewModel : PageModel
{
    private readonly ILogger<SubmitReviewModel> _logger;
    private readonly OrderService _orderService;
    private readonly ReviewService _reviewService;

    /// <summary>
    /// Order details for context.
    /// </summary>
    public OrderDetailsViewModel? Order { get; private set; }

    /// <summary>
    /// Product being reviewed.
    /// </summary>
    public OrderItemViewModel? Product { get; private set; }

    /// <summary>
    /// Store name for the product being reviewed.
    /// </summary>
    public string? StoreName { get; private set; }

    /// <summary>
    /// Review eligibility status.
    /// </summary>
    public ReviewEligibilityViewModel? Eligibility { get; private set; }

    [BindProperty]
    public Guid OrderId { get; set; }

    [BindProperty]
    public Guid ShipmentId { get; set; }

    [BindProperty]
    public Guid ProductId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please select a rating")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [BindProperty]
    [MaxLength(2000, ErrorMessage = "Review cannot exceed 2000 characters")]
    public string? Comment { get; set; }

    public SubmitReviewModel(
        ILogger<SubmitReviewModel> logger,
        OrderService orderService,
        ReviewService reviewService)
    {
        _logger = logger;
        _orderService = orderService;
        _reviewService = reviewService;
    }

    public async Task<IActionResult> OnGetAsync(
        Guid orderId, 
        Guid shipmentId, 
        Guid productId, 
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Login", new { returnUrl = $"/Buyer/SubmitReview?orderId={orderId}&shipmentId={shipmentId}&productId={productId}" });
        }

        var buyerId = GetBuyerId();
        if (!buyerId.HasValue)
        {
            return RedirectToPage("/Login");
        }

        // Check eligibility
        var eligibility = await _reviewService.HandleAsync(
            new CheckReviewEligibilityQuery(buyerId.Value, orderId, shipmentId, productId),
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            TempData["Error"] = eligibility.IneligibilityReason ?? "You cannot submit a review for this item.";
            return RedirectToPage("/Buyer/OrderDetails", new { orderId });
        }

        Eligibility = new ReviewEligibilityViewModel(
            eligibility.IsEligible,
            eligibility.IneligibilityReason,
            eligibility.HasExistingReview);

        // Get order details
        var orderDetails = await _orderService.HandleAsync(
            new GetBuyerOrderDetailsQuery(buyerId.Value, orderId),
            cancellationToken);

        if (orderDetails is null)
        {
            return RedirectToPage("/Buyer/Orders");
        }

        // Find the specific sub-order
        var subOrderDto = orderDetails.SellerSubOrders.FirstOrDefault(s => s.SubOrderId == shipmentId);
        if (subOrderDto is null)
        {
            return RedirectToPage("/Buyer/OrderDetails", new { orderId });
        }

        // Find the product being reviewed
        var productDto = subOrderDto.Items.FirstOrDefault(i => i.ProductId == productId);
        if (productDto is null)
        {
            TempData["Error"] = "Product not found in this order.";
            return RedirectToPage("/Buyer/OrderDetails", new { orderId });
        }

        // Map to ViewModels
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

        Product = new OrderItemViewModel(
            productDto.ProductId,
            productDto.ProductName,
            productDto.StoreId,
            productDto.StoreName,
            productDto.UnitPrice,
            productDto.Quantity,
            productDto.LineTotal,
            productDto.ShippingMethodName,
            productDto.ShippingCost,
            productDto.EstimatedDelivery);

        StoreName = subOrderDto.StoreName;

        // Set hidden form fields
        OrderId = orderId;
        ShipmentId = shipmentId;
        ProductId = productId;

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
            // Reload page data
            await OnGetAsync(OrderId, ShipmentId, ProductId, cancellationToken);
            return Page();
        }

        // Submit the review
        var result = await _reviewService.HandleAsync(
            new SubmitReviewCommand(
                buyerId.Value,
                OrderId,
                ShipmentId,
                ProductId,
                Rating,
                Comment),
            cancellationToken);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to submit review.";
            await OnGetAsync(OrderId, ShipmentId, ProductId, cancellationToken);
            return Page();
        }

        _logger.LogInformation(
            "Review submitted for product {ProductId} in order {OrderId} by buyer {BuyerId}",
            ProductId, OrderId, buyerId);

        TempData["Success"] = "Thank you for your review! It will be visible after moderation.";
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
