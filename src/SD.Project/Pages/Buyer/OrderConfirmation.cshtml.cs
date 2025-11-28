using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for displaying order confirmation after successful checkout.
/// </summary>
public class OrderConfirmationModel : PageModel
{
    private readonly ILogger<OrderConfirmationModel> _logger;
    private readonly CheckoutService _checkoutService;

    public OrderConfirmationDto? Order { get; private set; }

    public OrderConfirmationModel(
        ILogger<OrderConfirmationModel> logger,
        CheckoutService checkoutService)
    {
        _logger = logger;
        _checkoutService = checkoutService;
    }

    public async Task<IActionResult> OnGetAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // Verify user is authenticated
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Login", new { returnUrl = $"/Buyer/OrderConfirmation/{orderId}" });
        }

        var buyerId = GetBuyerId();
        if (!buyerId.HasValue)
        {
            return RedirectToPage("/Login");
        }

        // Get order confirmation details
        Order = await _checkoutService.HandleAsync(
            new GetOrderConfirmationQuery(buyerId.Value, orderId),
            cancellationToken);

        if (Order is null)
        {
            _logger.LogWarning("Order not found or access denied: {OrderId} for buyer {BuyerId}", orderId, buyerId);
        }
        else
        {
            _logger.LogInformation("Order confirmation viewed: {OrderNumber}", Order.OrderNumber);
        }

        return Page();
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
