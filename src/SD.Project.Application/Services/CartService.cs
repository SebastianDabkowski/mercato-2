using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating cart use cases.
/// </summary>
public sealed class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;

    public CartService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
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
        _cartRepository.Update(cart);
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
        _cartRepository.Update(cart);
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
        _cartRepository.Update(cart);
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

        _cartRepository.Update(cart);
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
            _cartRepository.Update(anonymousCart);
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
        _cartRepository.Delete(anonymousCart);
        _cartRepository.Update(buyerCart);
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

        // Get all stores
        var storeIds = cart.Items.Select(i => i.StoreId).Distinct().ToList();
        var stores = new Dictionary<Guid, Store>();
        foreach (var storeId in storeIds)
        {
            var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
            if (store is not null)
            {
                stores[storeId] = store;
            }
        }

        // Group items by seller
        var sellerGroups = new List<CartSellerGroupDto>();
        var itemsByStore = cart.GetItemsGroupedBySeller();

        foreach (var (storeId, items) in itemsByStore)
        {
            var store = stores.GetValueOrDefault(storeId);
            var itemDtos = new List<CartItemDto>();

            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    itemDtos.Add(MapToCartItemDto(item, product));
                }
            }

            if (itemDtos.Count > 0)
            {
                sellerGroups.Add(new CartSellerGroupDto(
                    storeId,
                    store?.Name ?? "Unknown Seller",
                    store?.Slug,
                    itemDtos.AsReadOnly(),
                    itemDtos.Sum(i => i.LineTotal)));
            }
        }

        var totalAmount = sellerGroups.Sum(g => g.Subtotal);

        return new CartDto(
            cart.Id,
            cart.BuyerId,
            sellerGroups.AsReadOnly(),
            cart.TotalItemCount,
            cart.UniqueItemCount,
            totalAmount,
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
