namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a product in a shopping cart.
/// </summary>
public class CartItem
{
    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }

    /// <summary>
    /// The seller's store ID. Used for grouping items by seller.
    /// </summary>
    public Guid StoreId { get; private set; }

    public int Quantity { get; private set; }

    /// <summary>
    /// The unit price of the product when it was added to the cart.
    /// Used for detecting price changes during checkout validation.
    /// </summary>
    public decimal UnitPriceAtAddition { get; private set; }

    /// <summary>
    /// The currency of the price when it was added to the cart.
    /// </summary>
    public string CurrencyAtAddition { get; private set; } = default!;

    public DateTime AddedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CartItem()
    {
        // EF Core constructor
    }

    public CartItem(Guid cartId, Guid productId, Guid storeId, int quantity, decimal unitPrice, string currency)
    {
        if (cartId == Guid.Empty)
        {
            throw new ArgumentException("Cart ID is required.", nameof(cartId));
        }

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

        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Id = Guid.NewGuid();
        CartId = cartId;
        ProductId = productId;
        StoreId = storeId;
        Quantity = quantity;
        UnitPriceAtAddition = unitPrice;
        CurrencyAtAddition = currency.ToUpperInvariant();
        AddedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the captured price when the quantity is changed.
    /// This should be called when the item quantity changes and price needs to be refreshed.
    /// </summary>
    public void UpdateCapturedPrice(decimal unitPrice, string currency)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        UnitPriceAtAddition = unitPrice;
        CurrencyAtAddition = currency.ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increases the quantity by the specified amount.
    /// </summary>
    public void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }

        Quantity += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the quantity to a specific value.
    /// </summary>
    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        Quantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
