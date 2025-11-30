using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of an order.
/// Statuses: new (Pending), paid (PaymentConfirmed), preparing (Processing), shipped, delivered, cancelled, refunded.
/// State transitions are constrained (e.g., cannot move from delivered back to preparing).
/// </summary>
public enum OrderStatus
{
    /// <summary>Order is newly created and awaiting payment confirmation (new).</summary>
    Pending,
    /// <summary>Payment has been authorized or confirmed (paid).</summary>
    PaymentConfirmed,
    /// <summary>Order is being prepared by sellers (preparing).</summary>
    Processing,
    /// <summary>All items have been shipped.</summary>
    Shipped,
    /// <summary>All items have been delivered.</summary>
    Delivered,
    /// <summary>Order was cancelled before shipment.</summary>
    Cancelled,
    /// <summary>Payment failed or was declined.</summary>
    PaymentFailed,
    /// <summary>Order has been refunded after a processed refund.</summary>
    Refunded
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
    /// Special instructions for delivery.
    /// </summary>
    public string? DeliveryInstructions { get; private set; }

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
    /// Idempotency key for payment provider retries.
    /// Ensures the same payment is not processed multiple times.
    /// </summary>
    public string? PaymentIdempotencyKey { get; private set; }

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
    public DateTime? RefundedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// The refunded amount if the order has been refunded.
    /// </summary>
    public decimal? RefundedAmount { get; private set; }

    /// <summary>
    /// Gets the payment status derived from the order status.
    /// This provides a simplified view of the payment lifecycle for buyers.
    /// </summary>
    public PaymentStatus PaymentStatus
    {
        get
        {
            return Status switch
            {
                OrderStatus.Pending => PaymentStatus.Pending,
                OrderStatus.PaymentFailed => PaymentStatus.Failed,
                OrderStatus.Cancelled when PaidAt.HasValue => PaymentStatus.Refunded,
                OrderStatus.Refunded => PaymentStatus.Refunded,
                _ when PaidAt.HasValue => PaymentStatus.Paid,
                _ => PaymentStatus.Pending
            };
        }
    }

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
        string? deliveryInstructions,
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
        DeliveryInstructions = deliveryInstructions?.Trim();
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
        decimal shippingCost = 0m,
        int? estimatedDeliveryDaysMin = null,
        int? estimatedDeliveryDaysMax = null)
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
            shippingCost,
            estimatedDeliveryDaysMin,
            estimatedDeliveryDaysMax);

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
    /// Sets the idempotency key for payment processing.
    /// Used to ensure duplicate payment requests are handled correctly.
    /// </summary>
    public void SetPaymentIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        PaymentIdempotencyKey = idempotencyKey;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the pending payment transaction ID.
    /// Used when payment is initiated but not yet confirmed (e.g., awaiting BLIK code).
    /// </summary>
    public void SetPendingTransactionId(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
        }

        PaymentTransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the order as payment confirmed (paid).
    /// Also sets all associated shipments to paid status.
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

        // Set all shipments to paid status
        foreach (var shipment in _shipments)
        {
            if (shipment.Status == ShipmentStatus.Pending)
            {
                shipment.ConfirmPayment();
            }
        }
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
    /// Cancels the order. Can only be done before shipment.
    /// Also cancels all associated shipments.
    /// </summary>
    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered || Status == OrderStatus.Refunded)
        {
            throw new InvalidOperationException($"Cannot cancel order in status {Status}.");
        }

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Cancel all shipments
        foreach (var shipment in _shipments)
        {
            if (shipment.Status != ShipmentStatus.Shipped && 
                shipment.Status != ShipmentStatus.Delivered && 
                shipment.Status != ShipmentStatus.Cancelled &&
                shipment.Status != ShipmentStatus.Refunded)
            {
                shipment.Cancel();
            }
        }
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

    /// <summary>
    /// Marks the order as shipped. Only valid when all shipments have shipped.
    /// </summary>
    public void MarkShipped()
    {
        if (Status != OrderStatus.Processing && Status != OrderStatus.PaymentConfirmed)
        {
            throw new InvalidOperationException($"Cannot mark order as shipped in status {Status}.");
        }

        // Verify all active shipments are shipped (cancelled/refunded shipments don't block)
        var activeShipments = _shipments.Where(s => 
            s.Status != ShipmentStatus.Cancelled && 
            s.Status != ShipmentStatus.Refunded);
        
        if (activeShipments.Any(s => s.Status != ShipmentStatus.Shipped && s.Status != ShipmentStatus.Delivered))
        {
            throw new InvalidOperationException("Cannot mark order as shipped until all active shipments are shipped.");
        }

        Status = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the order as delivered. Only valid when all shipments have been delivered.
    /// </summary>
    public void MarkDelivered()
    {
        if (Status != OrderStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot mark order as delivered in status {Status}.");
        }

        // Verify all active shipments are delivered (cancelled/refunded shipments don't block)
        var activeShipments = _shipments.Where(s => 
            s.Status != ShipmentStatus.Cancelled && 
            s.Status != ShipmentStatus.Refunded);
        
        if (activeShipments.Any(s => s.Status != ShipmentStatus.Delivered))
        {
            throw new InvalidOperationException("Cannot mark order as delivered until all active shipments are delivered.");
        }

        Status = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Refunds the order. Sets status to 'refunded' and records the refunded amount.
    /// Can be called after payment has been confirmed (paid, preparing, shipped, delivered).
    /// </summary>
    /// <param name="refundedAmount">The amount being refunded. If null, defaults to TotalAmount.</param>
    public void Refund(decimal? refundedAmount = null)
    {
        // Can only refund orders that have been paid
        if (Status == OrderStatus.Pending || Status == OrderStatus.PaymentFailed || Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot refund order in status {Status}.");
        }

        if (Status == OrderStatus.Refunded)
        {
            throw new InvalidOperationException("Order has already been refunded.");
        }

        var amount = refundedAmount ?? TotalAmount;
        if (amount <= 0)
        {
            throw new ArgumentException("Refunded amount must be greater than zero.", nameof(refundedAmount));
        }

        if (amount > TotalAmount)
        {
            throw new ArgumentException("Refunded amount cannot exceed total order amount.", nameof(refundedAmount));
        }

        Status = OrderStatus.Refunded;
        RefundedAmount = amount;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Refund shipments proportionally based on the refund amount
        var refundableShipments = _shipments
            .Where(s => s.Status != ShipmentStatus.Refunded && s.Status != ShipmentStatus.Cancelled)
            .ToList();
        
        if (refundableShipments.Count > 0)
        {
            var totalRefundableAmount = refundableShipments.Sum(s => s.Subtotal + s.ShippingCost);
            var refundRatio = totalRefundableAmount > 0 ? amount / totalRefundableAmount : 0;
            
            foreach (var shipment in refundableShipments)
            {
                var shipmentTotal = shipment.Subtotal + shipment.ShippingCost;
                var shipmentRefundAmount = shipmentTotal * refundRatio;
                
                // Full refund if ratio is 1 or very close
                if (refundRatio >= 1m || shipmentRefundAmount >= shipmentTotal)
                {
                    shipment.Refund();
                }
                else if (shipmentRefundAmount > 0)
                {
                    shipment.Refund(shipmentRefundAmount);
                }
            }
        }
    }

    /// <summary>
    /// Checks if the order can transition to the specified status.
    /// </summary>
    public bool CanTransitionTo(OrderStatus targetStatus)
    {
        return targetStatus switch
        {
            OrderStatus.Pending => false, // Cannot go back to pending
            OrderStatus.PaymentConfirmed => Status == OrderStatus.Pending,
            OrderStatus.Processing => Status == OrderStatus.PaymentConfirmed,
            OrderStatus.Shipped => Status == OrderStatus.Processing || Status == OrderStatus.PaymentConfirmed,
            OrderStatus.Delivered => Status == OrderStatus.Shipped,
            OrderStatus.Cancelled => CanBeCancelled(),
            OrderStatus.PaymentFailed => Status == OrderStatus.Pending,
            OrderStatus.Refunded => CanBeRefunded(),
            _ => false
        };
    }

    /// <summary>
    /// Checks if the order is in a state that allows cancellation.
    /// Orders cannot be cancelled once shipped, delivered, refunded, or already cancelled.
    /// </summary>
    private bool CanBeCancelled()
    {
        return Status != OrderStatus.Shipped && 
               Status != OrderStatus.Delivered && 
               Status != OrderStatus.Refunded && 
               Status != OrderStatus.Cancelled;
    }

    /// <summary>
    /// Checks if the order is in a state that allows refund.
    /// Orders can be refunded after payment is confirmed.
    /// </summary>
    private bool CanBeRefunded()
    {
        return Status == OrderStatus.PaymentConfirmed || 
               Status == OrderStatus.Processing || 
               Status == OrderStatus.Shipped || 
               Status == OrderStatus.Delivered;
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
