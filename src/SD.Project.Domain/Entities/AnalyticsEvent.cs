namespace SD.Project.Domain.Entities;

/// <summary>
/// Types of analytics events that can be tracked.
/// </summary>
public enum AnalyticsEventType
{
    /// <summary>Product search performed.</summary>
    Search,
    /// <summary>Product detail page viewed.</summary>
    ProductView,
    /// <summary>Product added to cart.</summary>
    AddToCart,
    /// <summary>Checkout process started.</summary>
    CheckoutStart,
    /// <summary>Order successfully completed.</summary>
    OrderComplete
}

/// <summary>
/// Represents an analytics event for tracking user actions.
/// Events are immutable once created and stored for Phase 2 analytics.
/// </summary>
public sealed class AnalyticsEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Type of the analytics event.
    /// </summary>
    public AnalyticsEventType EventType { get; private set; }

    /// <summary>
    /// Authenticated user ID (null for anonymous users).
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Session identifier for anonymous tracking.
    /// Used when UserId is null or for cross-session analysis.
    /// </summary>
    public string? SessionId { get; private set; }

    /// <summary>
    /// Product ID if the event relates to a specific product.
    /// </summary>
    public Guid? ProductId { get; private set; }

    /// <summary>
    /// Seller/Store ID if the event relates to a specific seller.
    /// </summary>
    public Guid? SellerId { get; private set; }

    /// <summary>
    /// Order ID for order-related events.
    /// </summary>
    public Guid? OrderId { get; private set; }

    /// <summary>
    /// Search term for search events.
    /// </summary>
    public string? SearchTerm { get; private set; }

    /// <summary>
    /// Quantity for add-to-cart events.
    /// </summary>
    public int? Quantity { get; private set; }

    /// <summary>
    /// Monetary value associated with the event (e.g., cart value, order total).
    /// </summary>
    public decimal? Value { get; private set; }

    /// <summary>
    /// Currency code for monetary values.
    /// </summary>
    public string? Currency { get; private set; }

    /// <summary>
    /// Additional metadata stored as JSON (for future extensibility).
    /// </summary>
    public string? Metadata { get; private set; }

    // EF Core constructor
    private AnalyticsEvent()
    {
    }

    private AnalyticsEvent(
        AnalyticsEventType eventType,
        Guid? userId,
        string? sessionId,
        Guid? productId = null,
        Guid? sellerId = null,
        Guid? orderId = null,
        string? searchTerm = null,
        int? quantity = null,
        decimal? value = null,
        string? currency = null,
        string? metadata = null)
    {
        if (userId is null && string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Either userId or sessionId must be provided.");
        }

        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        EventType = eventType;
        UserId = userId;
        SessionId = sessionId;
        ProductId = productId;
        SellerId = sellerId;
        OrderId = orderId;
        SearchTerm = searchTerm;
        Quantity = quantity;
        Value = value;
        Currency = currency;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a search event.
    /// </summary>
    public static AnalyticsEvent CreateSearchEvent(
        string searchTerm,
        Guid? userId,
        string? sessionId,
        int? resultCount = null)
    {
        return new AnalyticsEvent(
            AnalyticsEventType.Search,
            userId,
            sessionId,
            searchTerm: searchTerm,
            quantity: resultCount);
    }

    /// <summary>
    /// Creates a product view event.
    /// </summary>
    public static AnalyticsEvent CreateProductViewEvent(
        Guid productId,
        Guid? sellerId,
        Guid? userId,
        string? sessionId)
    {
        return new AnalyticsEvent(
            AnalyticsEventType.ProductView,
            userId,
            sessionId,
            productId: productId,
            sellerId: sellerId);
    }

    /// <summary>
    /// Creates an add-to-cart event.
    /// </summary>
    public static AnalyticsEvent CreateAddToCartEvent(
        Guid productId,
        Guid? sellerId,
        int quantity,
        decimal unitPrice,
        string currency,
        Guid? userId,
        string? sessionId)
    {
        return new AnalyticsEvent(
            AnalyticsEventType.AddToCart,
            userId,
            sessionId,
            productId: productId,
            sellerId: sellerId,
            quantity: quantity,
            value: unitPrice * quantity,
            currency: currency);
    }

    /// <summary>
    /// Creates a checkout start event.
    /// </summary>
    public static AnalyticsEvent CreateCheckoutStartEvent(
        decimal cartValue,
        string currency,
        int itemCount,
        Guid? userId,
        string? sessionId)
    {
        return new AnalyticsEvent(
            AnalyticsEventType.CheckoutStart,
            userId,
            sessionId,
            quantity: itemCount,
            value: cartValue,
            currency: currency);
    }

    /// <summary>
    /// Creates an order completion event.
    /// </summary>
    public static AnalyticsEvent CreateOrderCompleteEvent(
        Guid orderId,
        decimal orderTotal,
        string currency,
        int itemCount,
        Guid? userId,
        string? sessionId)
    {
        return new AnalyticsEvent(
            AnalyticsEventType.OrderComplete,
            userId,
            sessionId,
            orderId: orderId,
            quantity: itemCount,
            value: orderTotal,
            currency: currency);
    }
}
