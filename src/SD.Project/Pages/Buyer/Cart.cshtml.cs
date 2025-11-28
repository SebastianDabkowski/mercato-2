using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for displaying and managing the shopping cart.
/// </summary>
public class CartModel : PageModel
{
    private const string CartSessionKey = "CartSessionId";
    private readonly ILogger<CartModel> _logger;
    private readonly CartService _cartService;

    public CartViewModel? Cart { get; private set; }
    public string? Message { get; private set; }
    public bool IsSuccess { get; private set; }

    public CartModel(
        ILogger<CartModel> logger,
        CartService cartService)
    {
        _logger = logger;
        _cartService = cartService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var (buyerId, sessionId) = GetCartIdentifiers();

        var cartDto = await _cartService.HandleAsync(
            new GetCartQuery(buyerId, sessionId),
            cancellationToken);

        if (cartDto is not null)
        {
            Cart = MapToViewModel(cartDto);
            _logger.LogDebug("Loaded cart with {ItemCount} items", Cart.TotalItemCount);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var (buyerId, sessionId) = GetCartIdentifiers();

        var result = await _cartService.HandleAsync(
            new UpdateCartItemQuantityCommand(buyerId, sessionId, productId, quantity),
            cancellationToken);

        if (result.IsSuccess)
        {
            IsSuccess = true;
            Message = "Cart updated successfully.";
        }
        else
        {
            IsSuccess = false;
            Message = result.ErrorMessage;
        }

        return await OnGetAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostRemoveAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var (buyerId, sessionId) = GetCartIdentifiers();

        var result = await _cartService.HandleAsync(
            new RemoveFromCartCommand(buyerId, sessionId, productId),
            cancellationToken);

        if (result.IsSuccess)
        {
            IsSuccess = true;
            Message = "Item removed from cart.";
        }
        else
        {
            IsSuccess = false;
            Message = result.ErrorMessage;
        }

        return await OnGetAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostClearAsync(CancellationToken cancellationToken = default)
    {
        var (buyerId, sessionId) = GetCartIdentifiers();

        await _cartService.HandleAsync(
            new ClearCartCommand(buyerId, sessionId),
            cancellationToken);

        IsSuccess = true;
        Message = "Cart cleared.";

        return await OnGetAsync(cancellationToken);
    }

    private (Guid? BuyerId, string? SessionId) GetCartIdentifiers()
    {
        // Check if user is authenticated
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var buyerId))
            {
                return (buyerId, null);
            }
        }

        // For anonymous users, use session
        var sessionId = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString(CartSessionKey, sessionId);
        }

        return (null, sessionId);
    }

    private static CartViewModel MapToViewModel(Application.DTOs.CartDto dto)
    {
        // Determine currency (use first item's currency or default to USD)
        var currency = dto.SellerGroups
            .SelectMany(g => g.Items)
            .Select(i => i.Currency)
            .FirstOrDefault() ?? "USD";

        var sellerGroups = dto.SellerGroups.Select(g => new CartSellerGroupViewModel(
            g.StoreId,
            g.StoreName,
            g.StoreSlug,
            g.Items.Select(i => new CartItemViewModel(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.ProductDescription,
                i.UnitPrice,
                i.Currency,
                i.Quantity,
                i.LineTotal,
                i.AvailableStock,
                i.ProductImageUrl)).ToList().AsReadOnly(),
            g.Subtotal,
            currency)).ToList().AsReadOnly();

        return new CartViewModel(
            dto.Id,
            sellerGroups,
            dto.TotalItemCount,
            dto.UniqueItemCount,
            dto.TotalAmount,
            currency);
    }
}
