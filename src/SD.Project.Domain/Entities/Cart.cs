namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a shopping cart aggregate root.
/// Carts can contain items from multiple sellers.
/// </summary>
public class Cart
{
    private readonly List<CartItem> _items = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// The buyer's user ID. Null for anonymous/guest carts.
    /// </summary>
    public Guid? BuyerId { get; private set; }

    /// <summary>
    /// Session identifier for anonymous carts.
    /// </summary>
    public string? SessionId { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Cart()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a cart for a logged-in buyer.
    /// </summary>
    public Cart(Guid buyerId)
    {
        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required for authenticated carts.", nameof(buyerId));
        }

        Id = Guid.NewGuid();
        BuyerId = buyerId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a cart for an anonymous session.
    /// </summary>
    public Cart(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required for anonymous carts.", nameof(sessionId));
        }

        Id = Guid.NewGuid();
        SessionId = sessionId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a product to the cart. If the product already exists, quantity is increased.
    /// The price snapshot is updated to reflect the current price when adding more quantity.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The seller's store ID.</param>
    /// <param name="unitPrice">The current unit price of the product.</param>
    /// <param name="currency">The currency of the price.</param>
    /// <param name="quantity">Quantity to add.</param>
    /// <returns>The cart item that was added or updated.</returns>
    public CartItem AddItem(Guid productId, Guid storeId, decimal unitPrice, string currency, int quantity = 1)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem is not null)
        {
            existingItem.IncreaseQuantity(quantity);
            // Update the price snapshot to the current price when adding more items
            existingItem.UpdateCapturedPrice(unitPrice, currency);
            UpdatedAt = DateTime.UtcNow;
            return existingItem;
        }

        var newItem = new CartItem(Id, productId, storeId, quantity, unitPrice, currency);
        _items.Add(newItem);
        UpdatedAt = DateTime.UtcNow;
        return newItem;
    }

    /// <summary>
    /// Updates the quantity of an existing item.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The new quantity.</param>
    /// <returns>True if the item was updated; false if not found.</returns>
    public bool UpdateItemQuantity(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            return false;
        }

        item.SetQuantity(quantity);
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    /// <param name="productId">The product ID to remove.</param>
    /// <returns>True if the item was removed; false if not found.</returns>
    public bool RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets items grouped by seller/store.
    /// </summary>
    public IReadOnlyDictionary<Guid, IReadOnlyCollection<CartItem>> GetItemsGroupedBySeller()
    {
        return _items
            .GroupBy(i => i.StoreId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<CartItem>)g.ToList().AsReadOnly());
    }

    /// <summary>
    /// Gets the total number of items in the cart.
    /// </summary>
    public int TotalItemCount => _items.Sum(i => i.Quantity);

    /// <summary>
    /// Gets the number of unique products in the cart.
    /// </summary>
    public int UniqueItemCount => _items.Count;

    /// <summary>
    /// Associates the cart with a buyer (for cart migration from anonymous to authenticated).
    /// </summary>
    /// <param name="buyerId">The buyer's user ID.</param>
    public void AssociateBuyer(Guid buyerId)
    {
        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        BuyerId = buyerId;
        SessionId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds the items from the collection to the cart.
    /// Used when loading cart items from persistence.
    /// </summary>
    public void LoadItems(IEnumerable<CartItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }
}
