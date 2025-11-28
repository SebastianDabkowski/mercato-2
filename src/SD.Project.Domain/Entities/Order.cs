namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order is being created during checkout.</summary>
    Pending,
    /// <summary>Payment has been authorized or confirmed.</summary>
    PaymentConfirmed,
    /// <summary>Order is being processed by sellers.</summary>
    Processing,
    /// <summary>All items have been shipped.</summary>
    Shipped,
    /// <summary>All items have been delivered.</summary>
    Delivered,
    /// <summary>Order was cancelled before shipment.</summary>
    Cancelled,
    /// <summary>Payment failed or was declined.</summary>
    PaymentFailed
}

/// <summary>
/// Represents an order created from checkout.
/// An order can contain items from multiple sellers.
/// </summary>
public class Order
{
    private readonly List<OrderItem> _items = new();
    private readonly List<OrderShipment> _shipments = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// The buyer's user ID.
    /// </summary>
    public Guid BuyerId { get; private set; }

    /// <summary>
    /// Reference number for the order (human-readable).
    /// </summary>
    public string OrderNumber { get; private set; } = default!;

    /// <summary>
    /// Current status of the order.
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Delivery address for the order.
    /// </summary>
    public Guid DeliveryAddressId { get; private set; }

    /// <summary>
    /// Denormalized delivery address fields for historical record.
    /// </summary>
    public string RecipientName { get; private set; } = default!;
    public string DeliveryStreet { get; private set; } = default!;
    public string? DeliveryStreet2 { get; private set; }
    public string DeliveryCity { get; private set; } = default!;
    public string? DeliveryState { get; private set; }
    public string DeliveryPostalCode { get; private set; } = default!;
    public string DeliveryCountry { get; private set; } = default!;
    public string? DeliveryPhoneNumber { get; private set; }

    /// <summary>
    /// Selected payment method ID.
    /// </summary>
    public Guid PaymentMethodId { get; private set; }

    /// <summary>
    /// Payment method name for display.
    /// </summary>
    public string PaymentMethodName { get; private set; } = default!;

    /// <summary>
    /// External payment transaction/reference ID from payment provider.
    /// </summary>
    public string? PaymentTransactionId { get; private set; }

    /// <summary>
    /// Items subtotal before shipping.
    /// </summary>
    public decimal ItemSubtotal { get; private set; }

    /// <summary>
    /// Total shipping cost across all sellers.
    /// </summary>
    public decimal TotalShipping { get; private set; }

    /// <summary>
    /// Total order amount (items + shipping).
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Currency code for all amounts.
    /// </summary>
    public string Currency { get; private set; } = default!;

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderShipment> Shipments => _shipments.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Order()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new order from checkout.
    /// </summary>
    public Order(
        Guid buyerId,
        string orderNumber,
        Guid deliveryAddressId,
        string recipientName,
        string deliveryStreet,
        string? deliveryStreet2,
        string deliveryCity,
        string? deliveryState,
        string deliveryPostalCode,
        string deliveryCountry,
        string? deliveryPhoneNumber,
        Guid paymentMethodId,
        string paymentMethodName,
        string currency)
    {
        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new ArgumentException("Order number is required.", nameof(orderNumber));
        }

        if (deliveryAddressId == Guid.Empty)
        {
            throw new ArgumentException("Delivery address ID is required.", nameof(deliveryAddressId));
        }

        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new ArgumentException("Recipient name is required.", nameof(recipientName));
        }

        if (paymentMethodId == Guid.Empty)
        {
            throw new ArgumentException("Payment method ID is required.", nameof(paymentMethodId));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Id = Guid.NewGuid();
        BuyerId = buyerId;
        OrderNumber = orderNumber;
        Status = OrderStatus.Pending;
        DeliveryAddressId = deliveryAddressId;
        RecipientName = recipientName;
        DeliveryStreet = deliveryStreet;
        DeliveryStreet2 = deliveryStreet2;
        DeliveryCity = deliveryCity;
        DeliveryState = deliveryState;
        DeliveryPostalCode = deliveryPostalCode;
        DeliveryCountry = deliveryCountry;
        DeliveryPhoneNumber = deliveryPhoneNumber;
        PaymentMethodId = paymentMethodId;
        PaymentMethodName = paymentMethodName;
        Currency = currency.ToUpperInvariant();
        ItemSubtotal = 0m;
        TotalShipping = 0m;
        TotalAmount = 0m;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an item to the order.
    /// </summary>
    public OrderItem AddItem(
        Guid productId,
        Guid storeId,
        string productName,
        decimal unitPrice,
        int quantity,
        Guid? shippingMethodId = null,
        string? shippingMethodName = null,
        decimal shippingCost = 0m)
    {
        var item = new OrderItem(
            Id,
            productId,
            storeId,
            productName,
            unitPrice,
            quantity,
            shippingMethodId,
            shippingMethodName,
            shippingCost);

        _items.Add(item);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
        return item;
    }

    /// <summary>
    /// Creates shipments grouped by seller.
    /// </summary>
    public void CreateShipments()
    {
        _shipments.Clear();

        var itemsByStore = _items.GroupBy(i => i.StoreId);
        foreach (var group in itemsByStore)
        {
            var storeItems = group.ToList();
            var shippingCost = storeItems.Sum(i => i.ShippingCost);
            var subtotal = storeItems.Sum(i => i.LineTotal);

            var shipment = new OrderShipment(
                Id,
                group.Key,
                subtotal,
                shippingCost);

            _shipments.Add(shipment);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the order as payment confirmed.
    /// </summary>
    public void ConfirmPayment(string? transactionId)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot confirm payment for order in status {Status}.");
        }

        Status = OrderStatus.PaymentConfirmed;
        PaymentTransactionId = transactionId;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the order as payment failed.
    /// </summary>
    public void FailPayment()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot fail payment for order in status {Status}.");
        }

        Status = OrderStatus.PaymentFailed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the order.
    /// </summary>
    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException($"Cannot cancel order in status {Status}.");
        }

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Moves order to processing status.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != OrderStatus.PaymentConfirmed)
        {
            throw new InvalidOperationException($"Cannot start processing order in status {Status}.");
        }

        Status = OrderStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        ItemSubtotal = _items.Sum(i => i.LineTotal);
        TotalShipping = _items.Sum(i => i.ShippingCost);
        TotalAmount = ItemSubtotal + TotalShipping;
    }

    /// <summary>
    /// Loads items from persistence.
    /// </summary>
    public void LoadItems(IEnumerable<OrderItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    /// <summary>
    /// Loads shipments from persistence.
    /// </summary>
    public void LoadShipments(IEnumerable<OrderShipment> shipments)
    {
        _shipments.Clear();
        _shipments.AddRange(shipments);
    }
}
