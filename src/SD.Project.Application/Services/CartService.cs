using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.Services;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating cart use cases.
/// </summary>
public sealed class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IShippingRuleRepository _shippingRuleRepository;
    private readonly CartTotalsCalculator _cartTotalsCalculator;

    public CartService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IShippingRuleRepository shippingRuleRepository,
        CartTotalsCalculator cartTotalsCalculator)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _shippingRuleRepository = shippingRuleRepository;
        _cartTotalsCalculator = cartTotalsCalculator;
    }

    /// <summary>
    /// Gets the current cart for the buyer or session.
    /// </summary>
    public async Task<CartDto?> HandleAsync(GetCartQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cart = await GetOrCreateCartAsync(query.BuyerId, query.SessionId, createIfNotExists: false, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        return await MapToCartDtoAsync(cart, cancellationToken);
    }

    /// <summary>
    /// Gets the item count in the cart.
    /// </summary>
    public async Task<int> HandleAsync(GetCartItemCountQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cart = await GetOrCreateCartAsync(query.BuyerId, query.SessionId, createIfNotExists: false, cancellationToken);
        return cart?.TotalItemCount ?? 0;
    }

    /// <summary>
    /// Adds a product to the cart.
    /// </summary>
    public async Task<AddToCartResultDto> HandleAsync(AddToCartCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate product exists and is available
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return AddToCartResultDto.Failed("Product not found.");
        }

        if (product.Status != ProductStatus.Active || !product.IsActive)
        {
            return AddToCartResultDto.Failed("Product is not available for purchase.");
        }

        if (!product.StoreId.HasValue)
        {
            return AddToCartResultDto.Failed("Product does not belong to a store.");
        }

        if (product.Stock < command.Quantity)
        {
            return AddToCartResultDto.Failed($"Insufficient stock. Only {product.Stock} available.");
        }

        // Get or create cart
        var cart = await GetOrCreateCartAsync(command.BuyerId, command.SessionId, createIfNotExists: true, cancellationToken);
        if (cart is null)
        {
            return AddToCartResultDto.Failed("Could not create cart. Please provide buyer ID or session ID.");
        }

        // Check if item already exists
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == command.ProductId);
        var wasQuantityIncreased = existingItem is not null;

        // Check total quantity won't exceed stock
        var totalQuantity = (existingItem?.Quantity ?? 0) + command.Quantity;
        if (totalQuantity > product.Stock)
        {
            return AddToCartResultDto.Failed($"Cannot add {command.Quantity} more. You have {existingItem?.Quantity ?? 0} in cart and only {product.Stock} available.");
        }

        // Add item to cart
        var cartItem = cart.AddItem(command.ProductId, product.StoreId.Value, command.Quantity);

        // Save changes
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _cartRepository.SaveChangesAsync(cancellationToken);

        // Map to DTO
        var itemDto = MapToCartItemDto(cartItem, product);
        return AddToCartResultDto.Succeeded(itemDto, wasQuantityIncreased);
    }

    /// <summary>
    /// Updates the quantity of an item in the cart.
    /// </summary>
    public async Task<UpdateCartItemResultDto> HandleAsync(UpdateCartItemQuantityCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Quantity <= 0)
        {
            return UpdateCartItemResultDto.Failed("Quantity must be greater than zero.");
        }

        var cart = await GetOrCreateCartAsync(command.BuyerId, command.SessionId, createIfNotExists: false, cancellationToken);
        if (cart is null)
        {
            return UpdateCartItemResultDto.Failed("Cart not found.");
        }

        var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == command.ProductId);
        if (cartItem is null)
        {
            return UpdateCartItemResultDto.Failed("Item not found in cart.");
        }

        // Validate stock
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return UpdateCartItemResultDto.Failed("Product no longer exists.");
        }

        if (command.Quantity > product.Stock)
        {
            return UpdateCartItemResultDto.Failed($"Cannot set quantity to {command.Quantity}. Only {product.Stock} available.");
        }

        // Update quantity
        cart.UpdateItemQuantity(command.ProductId, command.Quantity);

        // Save changes
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _cartRepository.SaveChangesAsync(cancellationToken);

        // Refresh cart item reference after update
        cartItem = cart.Items.First(i => i.ProductId == command.ProductId);
        var itemDto = MapToCartItemDto(cartItem, product);
        return UpdateCartItemResultDto.Succeeded(itemDto);
    }

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    public async Task<RemoveFromCartResultDto> HandleAsync(RemoveFromCartCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cart = await GetOrCreateCartAsync(command.BuyerId, command.SessionId, createIfNotExists: false, cancellationToken);
        if (cart is null)
        {
            return RemoveFromCartResultDto.Failed("Cart not found.");
        }

        var removed = cart.RemoveItem(command.ProductId);
        if (!removed)
        {
            return RemoveFromCartResultDto.Failed("Item not found in cart.");
        }

        // Save changes
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _cartRepository.SaveChangesAsync(cancellationToken);

        return RemoveFromCartResultDto.Succeeded();
    }

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    public async Task HandleAsync(ClearCartCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cart = await GetOrCreateCartAsync(command.BuyerId, command.SessionId, createIfNotExists: false, cancellationToken);
        if (cart is null)
        {
            return;
        }

        cart.Clear();

        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _cartRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Merges an anonymous cart into a buyer's cart upon login.
    /// </summary>
    public async Task HandleAsync(MergeCartsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.BuyerId == Guid.Empty)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(command.SessionId))
        {
            return;
        }

        // Get anonymous cart
        var anonymousCart = await _cartRepository.GetBySessionIdAsync(command.SessionId, cancellationToken);
        if (anonymousCart is null || anonymousCart.Items.Count == 0)
        {
            return;
        }

        // Get or create buyer's cart
        var buyerCart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId, cancellationToken);
        if (buyerCart is null)
        {
            // Just associate the anonymous cart with the buyer
            anonymousCart.AssociateBuyer(command.BuyerId);
            await _cartRepository.UpdateAsync(anonymousCart, cancellationToken);
            await _cartRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        // Merge items from anonymous cart to buyer's cart
        foreach (var item in anonymousCart.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null || product.Status != ProductStatus.Active)
            {
                continue;
            }

            var existingItem = buyerCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            var totalQuantity = (existingItem?.Quantity ?? 0) + item.Quantity;

            // Cap at available stock
            var quantityToAdd = Math.Min(item.Quantity, product.Stock - (existingItem?.Quantity ?? 0));
            if (quantityToAdd > 0 && product.StoreId.HasValue)
            {
                buyerCart.AddItem(item.ProductId, product.StoreId.Value, quantityToAdd);
            }
        }

        // Delete anonymous cart and save buyer's cart
        await _cartRepository.DeleteAsync(anonymousCart, cancellationToken);
        await _cartRepository.UpdateAsync(buyerCart, cancellationToken);
        await _cartRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Cart?> GetOrCreateCartAsync(
        Guid? buyerId,
        string? sessionId,
        bool createIfNotExists,
        CancellationToken cancellationToken)
    {
        Cart? cart = null;

        // Try to get by buyer ID first
        if (buyerId.HasValue && buyerId.Value != Guid.Empty)
        {
            cart = await _cartRepository.GetByBuyerIdAsync(buyerId.Value, cancellationToken);
            if (cart is null && createIfNotExists)
            {
                cart = new Cart(buyerId.Value);
                await _cartRepository.AddAsync(cart, cancellationToken);
                await _cartRepository.SaveChangesAsync(cancellationToken);
            }
            return cart;
        }

        // Try to get by session ID
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            cart = await _cartRepository.GetBySessionIdAsync(sessionId, cancellationToken);
            if (cart is null && createIfNotExists)
            {
                cart = new Cart(sessionId);
                await _cartRepository.AddAsync(cart, cancellationToken);
                await _cartRepository.SaveChangesAsync(cancellationToken);
            }
            return cart;
        }

        return null;
    }

    private async Task<CartDto> MapToCartDtoAsync(Cart cart, CancellationToken cancellationToken)
    {
        // Get all products in cart
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);

        // Get all stores in a single batch query
        var storeIds = cart.Items.Select(i => i.StoreId).Distinct().ToList();
        var storesList = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var stores = storesList.ToDictionary(s => s.Id);

        // Get shipping rules for all stores in a single batch query
        var shippingRules = await _shippingRuleRepository.GetDefaultsByStoreIdsAsync(storeIds, cancellationToken);

        // Determine currency from products. In a multi-currency marketplace, 
        // products in a single cart should share the same currency.
        // Fallback to USD if cart is empty or products have no price.
        var currency = products.FirstOrDefault()?.Price.Currency ?? "USD";

        // Group items by seller and calculate totals
        var sellerGroups = new List<CartSellerGroupDto>();
        var sellerTotals = new List<SellerCartTotals>();
        var itemsByStore = cart.GetItemsGroupedBySeller();

        foreach (var (storeId, items) in itemsByStore)
        {
            var store = stores.GetValueOrDefault(storeId);
            var shippingRule = shippingRules.GetValueOrDefault(storeId);
            var itemDtos = new List<CartItemDto>();
            decimal sellerSubtotal = 0m;
            int sellerItemCount = 0;

            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    itemDtos.Add(MapToCartItemDto(item, product));
                    sellerSubtotal += product.Price.Amount * item.Quantity;
                    sellerItemCount += item.Quantity;
                }
            }

            if (itemDtos.Count > 0)
            {
                // Calculate shipping for this seller
                var sellerTotal = _cartTotalsCalculator.CalculateSellerTotals(
                    storeId,
                    sellerSubtotal,
                    sellerItemCount,
                    currency,
                    shippingRule);
                sellerTotals.Add(sellerTotal);

                sellerGroups.Add(new CartSellerGroupDto(
                    storeId,
                    store?.Name ?? "Unknown Seller",
                    store?.Slug,
                    itemDtos.AsReadOnly(),
                    sellerSubtotal,
                    sellerTotal.Shipping.Amount,
                    sellerTotal.Total.Amount));
            }
        }

        // Calculate overall cart totals
        var cartTotals = _cartTotalsCalculator.CalculateCartTotals(sellerTotals, currency);

        return new CartDto(
            cart.Id,
            cart.BuyerId,
            sellerGroups.AsReadOnly(),
            cart.TotalItemCount,
            cart.UniqueItemCount,
            cartTotals.ItemSubtotal.Amount,
            cartTotals.TotalShipping.Amount,
            cartTotals.TotalAmount.Amount,
            currency,
            cart.CreatedAt,
            cart.UpdatedAt);
    }

    private static CartItemDto MapToCartItemDto(CartItem item, Product product)
    {
        return new CartItemDto(
            item.Id,
            item.ProductId,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            item.Quantity,
            product.Price.Amount * item.Quantity,
            product.Stock,
            null, // Image URL - can be extended later
            item.AddedAt);
    }
}
