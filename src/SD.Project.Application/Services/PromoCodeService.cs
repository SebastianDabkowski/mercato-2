using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.Services;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing promo code operations.
/// </summary>
public sealed class PromoCodeService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPromoCodeRepository _promoCodeRepository;
    private readonly IShippingRuleRepository _shippingRuleRepository;
    private readonly PromoCodeValidator _promoCodeValidator;
    private readonly CartTotalsCalculator _cartTotalsCalculator;

    public PromoCodeService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IPromoCodeRepository promoCodeRepository,
        IShippingRuleRepository shippingRuleRepository,
        PromoCodeValidator promoCodeValidator,
        CartTotalsCalculator cartTotalsCalculator)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _promoCodeRepository = promoCodeRepository;
        _shippingRuleRepository = shippingRuleRepository;
        _promoCodeValidator = promoCodeValidator;
        _cartTotalsCalculator = cartTotalsCalculator;
    }

    /// <summary>
    /// Applies a promo code to the cart.
    /// </summary>
    public async Task<ApplyPromoCodeResultDto> HandleAsync(
        ApplyPromoCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.PromoCode))
        {
            return ApplyPromoCodeResultDto.Failed("Please enter a promo code.");
        }

        // Get cart
        var cart = await GetCartAsync(command.BuyerId, command.SessionId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return ApplyPromoCodeResultDto.Failed("Your cart is empty.");
        }

        // Check if a promo code is already applied
        if (cart.HasPromoCodeApplied)
        {
            return ApplyPromoCodeResultDto.AlreadyApplied(cart.AppliedPromoCode!);
        }

        // Get the promo code
        var promoCode = await _promoCodeRepository.GetByCodeAsync(command.PromoCode.Trim(), cancellationToken);
        if (promoCode is null)
        {
            return ApplyPromoCodeResultDto.Failed("Invalid promo code.");
        }

        // Calculate cart data for validation
        var (cartSubtotal, cartCurrency, storeSubtotals) = await CalculateCartDataAsync(cart, cancellationToken);

        // Get user usage count if buyer is authenticated
        var userUsageCount = 0;
        if (command.BuyerId.HasValue && command.BuyerId.Value != Guid.Empty)
        {
            userUsageCount = await _promoCodeRepository.GetUserUsageCountAsync(
                promoCode.Id, command.BuyerId.Value, cancellationToken);
        }

        // Validate the promo code
        var validationResult = _promoCodeValidator.Validate(
            promoCode,
            cartSubtotal,
            cartCurrency,
            storeSubtotals,
            userUsageCount);

        if (!validationResult.IsValid)
        {
            return ApplyPromoCodeResultDto.Failed(validationResult.ErrorMessage!);
        }

        // Apply the promo code to the cart
        cart.ApplyPromoCode(
            validationResult.PromoCodeId!.Value,
            validationResult.PromoCode!,
            validationResult.DiscountAmount,
            validationResult.DiscountDescription!);

        // Calculate new total
        var newTotal = await CalculateNewTotalAsync(cart, cancellationToken);

        // Save changes
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _cartRepository.SaveChangesAsync(cancellationToken);

        return ApplyPromoCodeResultDto.Success(
            validationResult.PromoCodeId!.Value,
            validationResult.PromoCode!,
            validationResult.DiscountAmount,
            validationResult.DiscountDescription!,
            newTotal);
    }

    /// <summary>
    /// Removes the applied promo code from the cart.
    /// </summary>
    public async Task<RemovePromoCodeResultDto> HandleAsync(
        RemovePromoCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get cart
        var cart = await GetCartAsync(command.BuyerId, command.SessionId, cancellationToken);
        if (cart is null)
        {
            return RemovePromoCodeResultDto.Failed("Cart not found.");
        }

        if (!cart.HasPromoCodeApplied)
        {
            return RemovePromoCodeResultDto.NoPromoApplied();
        }

        // Remove the promo code
        cart.ClearPromoCode();

        // Calculate new total
        var newTotal = await CalculateNewTotalAsync(cart, cancellationToken);

        // Save changes
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _cartRepository.SaveChangesAsync(cancellationToken);

        return RemovePromoCodeResultDto.Success(newTotal);
    }

    /// <summary>
    /// Revalidates the applied promo code (called when cart changes).
    /// If the promo code is no longer valid, it is automatically removed.
    /// </summary>
    public async Task RevalidateAppliedPromoAsync(
        Cart cart,
        Guid? buyerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (!cart.HasPromoCodeApplied)
        {
            return;
        }

        // Get the promo code
        var promoCode = await _promoCodeRepository.GetByIdAsync(cart.AppliedPromoCodeId!.Value, cancellationToken);
        if (promoCode is null)
        {
            cart.ClearPromoCode();
            return;
        }

        // Calculate current cart data
        var (cartSubtotal, cartCurrency, storeSubtotals) = await CalculateCartDataAsync(cart, cancellationToken);

        // Get user usage count if buyer is authenticated
        var userUsageCount = 0;
        if (buyerId.HasValue && buyerId.Value != Guid.Empty)
        {
            userUsageCount = await _promoCodeRepository.GetUserUsageCountAsync(
                promoCode.Id, buyerId.Value, cancellationToken);
        }

        // Validate the promo code
        var validationResult = _promoCodeValidator.Validate(
            promoCode,
            cartSubtotal,
            cartCurrency,
            storeSubtotals,
            userUsageCount);

        if (!validationResult.IsValid)
        {
            // Promo code is no longer valid, remove it
            cart.ClearPromoCode();
        }
        else
        {
            // Update discount amount in case cart subtotal changed
            cart.ApplyPromoCode(
                validationResult.PromoCodeId!.Value,
                validationResult.PromoCode!,
                validationResult.DiscountAmount,
                validationResult.DiscountDescription!);
        }
    }

    private async Task<Cart?> GetCartAsync(Guid? buyerId, string? sessionId, CancellationToken cancellationToken)
    {
        if (buyerId.HasValue && buyerId.Value != Guid.Empty)
        {
            return await _cartRepository.GetByBuyerIdAsync(buyerId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return await _cartRepository.GetBySessionIdAsync(sessionId, cancellationToken);
        }

        return null;
    }

    private async Task<(decimal Subtotal, string Currency, Dictionary<Guid, decimal> StoreSubtotals)> CalculateCartDataAsync(
        Cart cart,
        CancellationToken cancellationToken)
    {
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);

        var currency = products.FirstOrDefault()?.Price.Currency ?? "USD";
        var storeSubtotals = new Dictionary<Guid, decimal>();
        decimal totalSubtotal = 0m;

        var itemsByStore = cart.GetItemsGroupedBySeller();
        foreach (var (storeId, items) in itemsByStore)
        {
            decimal sellerSubtotal = 0m;
            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    sellerSubtotal += product.Price.Amount * item.Quantity;
                }
            }
            storeSubtotals[storeId] = sellerSubtotal;
            totalSubtotal += sellerSubtotal;
        }

        return (totalSubtotal, currency, storeSubtotals);
    }

    private async Task<decimal> CalculateNewTotalAsync(Cart cart, CancellationToken cancellationToken)
    {
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);

        var storeIds = cart.Items.Select(i => i.StoreId).Distinct().ToList();
        var shippingRules = await _shippingRuleRepository.GetDefaultsByStoreIdsAsync(storeIds, cancellationToken);

        var currency = products.FirstOrDefault()?.Price.Currency ?? "USD";
        var sellerTotals = new List<SellerCartTotals>();

        var itemsByStore = cart.GetItemsGroupedBySeller();
        foreach (var (storeId, items) in itemsByStore)
        {
            var shippingRule = shippingRules.GetValueOrDefault(storeId);
            decimal sellerSubtotal = 0m;
            int sellerItemCount = 0;

            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    sellerSubtotal += product.Price.Amount * item.Quantity;
                    sellerItemCount += item.Quantity;
                }
            }

            var sellerTotal = _cartTotalsCalculator.CalculateSellerTotals(
                storeId,
                sellerSubtotal,
                sellerItemCount,
                currency,
                shippingRule);
            sellerTotals.Add(sellerTotal);
        }

        var cartTotals = _cartTotalsCalculator.CalculateCartTotals(
            sellerTotals, 
            currency, 
            cart.PromoDiscountAmount);

        return cartTotals.TotalAmount.Amount;
    }
}
