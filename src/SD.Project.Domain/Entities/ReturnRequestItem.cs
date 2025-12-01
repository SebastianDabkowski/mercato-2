namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an item included in a return/complaint request.
/// Links a return request to specific order items.
/// </summary>
public class ReturnRequestItem
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The return request this item belongs to.
    /// </summary>
    public Guid ReturnRequestId { get; private set; }

    /// <summary>
    /// The order item being returned/complained about.
    /// </summary>
    public Guid OrderItemId { get; private set; }

    /// <summary>
    /// Product name at time of request (denormalized for historical record).
    /// </summary>
    public string ProductName { get; private set; } = default!;

    /// <summary>
    /// Quantity being returned/complained about.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// When this item was added to the request.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private ReturnRequestItem()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new return request item.
    /// </summary>
    public ReturnRequestItem(Guid returnRequestId, Guid orderItemId, string productName, int quantity)
    {
        if (returnRequestId == Guid.Empty)
        {
            throw new ArgumentException("Return request ID is required.", nameof(returnRequestId));
        }

        if (orderItemId == Guid.Empty)
        {
            throw new ArgumentException("Order item ID is required.", nameof(orderItemId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        Id = Guid.NewGuid();
        ReturnRequestId = returnRequestId;
        OrderItemId = orderItemId;
        ProductName = productName.Trim();
        Quantity = quantity;
        CreatedAt = DateTime.UtcNow;
    }
}
