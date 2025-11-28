namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an item in an order.
/// </summary>
public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid StoreId { get; private set; }

    /// <summary>
    /// Product name at time of order (denormalized for historical record).
    /// </summary>
    public string ProductName { get; private set; } = default!;

    /// <summary>
    /// Unit price at time of order.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    /// <summary>
    /// Line total (unit price × quantity).
    /// </summary>
    public decimal LineTotal { get; private set; }

    /// <summary>
    /// Selected shipping method for this item/seller.
    /// </summary>
    public Guid? ShippingMethodId { get; private set; }

    /// <summary>
    /// Shipping method name for display.
    /// </summary>
    public string? ShippingMethodName { get; private set; }

    /// <summary>
    /// Shipping cost for this item.
    /// </summary>
    public decimal ShippingCost { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private OrderItem()
    {
        // EF Core constructor
    }

    public OrderItem(
        Guid orderId,
        Guid productId,
        Guid storeId,
        string productName,
        decimal unitPrice,
        int quantity,
        Guid? shippingMethodId = null,
        string? shippingMethodName = null,
        decimal shippingCost = 0m)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (shippingCost < 0)
        {
            throw new ArgumentException("Shipping cost cannot be negative.", nameof(shippingCost));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        StoreId = storeId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = unitPrice * quantity;
        ShippingMethodId = shippingMethodId;
        ShippingMethodName = shippingMethodName;
        ShippingCost = shippingCost;
        CreatedAt = DateTime.UtcNow;
    }
}
