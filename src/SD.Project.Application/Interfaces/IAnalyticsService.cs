namespace SD.Project.Application.Interfaces;

/// <summary>
/// Abstraction for analytics event tracking.
/// Implementations can write to various backends (database, external analytics, etc.).
/// Tracking is fire-and-forget to avoid impacting core application flows.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Tracks a product search event.
    /// </summary>
    /// <param name="searchTerm">The search term used.</param>
    /// <param name="resultCount">Number of results returned.</param>
    /// <param name="userId">Authenticated user ID (null for anonymous).</param>
    /// <param name="sessionId">Session identifier for anonymous tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackSearchAsync(
        string searchTerm,
        int resultCount,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a product view event.
    /// </summary>
    /// <param name="productId">The ID of the viewed product.</param>
    /// <param name="sellerId">The ID of the product's seller/store.</param>
    /// <param name="userId">Authenticated user ID (null for anonymous).</param>
    /// <param name="sessionId">Session identifier for anonymous tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackProductViewAsync(
        Guid productId,
        Guid? sellerId,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an add-to-cart event.
    /// </summary>
    /// <param name="productId">The ID of the product added.</param>
    /// <param name="sellerId">The ID of the product's seller/store.</param>
    /// <param name="quantity">Quantity added to cart.</param>
    /// <param name="unitPrice">Unit price of the product.</param>
    /// <param name="currency">Currency code.</param>
    /// <param name="userId">Authenticated user ID (null for anonymous).</param>
    /// <param name="sessionId">Session identifier for anonymous tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackAddToCartAsync(
        Guid productId,
        Guid? sellerId,
        int quantity,
        decimal unitPrice,
        string currency,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a checkout start event.
    /// </summary>
    /// <param name="cartValue">Total value of the cart.</param>
    /// <param name="currency">Currency code.</param>
    /// <param name="itemCount">Number of items in cart.</param>
    /// <param name="userId">Authenticated user ID (null for anonymous).</param>
    /// <param name="sessionId">Session identifier for anonymous tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackCheckoutStartAsync(
        decimal cartValue,
        string currency,
        int itemCount,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an order completion event.
    /// </summary>
    /// <param name="orderId">The ID of the completed order.</param>
    /// <param name="orderTotal">Total value of the order.</param>
    /// <param name="currency">Currency code.</param>
    /// <param name="itemCount">Number of items in the order.</param>
    /// <param name="userId">Authenticated user ID (null for anonymous).</param>
    /// <param name="sessionId">Session identifier for anonymous tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackOrderCompleteAsync(
        Guid orderId,
        decimal orderTotal,
        string currency,
        int itemCount,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default);
}
