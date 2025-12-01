using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Configuration options for analytics tracking.
/// </summary>
public sealed class AnalyticsOptions
{
    /// <summary>
    /// Whether analytics tracking is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to log events to the application logger.
    /// Default is true.
    /// </summary>
    public bool LogToConsole { get; set; } = true;

    /// <summary>
    /// Whether to persist events to the database.
    /// Default is true.
    /// </summary>
    public bool PersistToDatabase { get; set; } = true;
}

/// <summary>
/// Implementation of the analytics service that records events.
/// Events are recorded to the database and optionally logged.
/// Tracking is fire-and-forget to avoid impacting core application flows.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsEventRepository _repository;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly AnalyticsOptions _options;

    public AnalyticsService(
        IAnalyticsEventRepository repository,
        ILogger<AnalyticsService> logger,
        IOptions<AnalyticsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _repository = repository;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task TrackSearchAsync(
        string searchTerm,
        int resultCount,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (!ValidateIdentity(userId, sessionId))
        {
            return;
        }

        try
        {
            var analyticsEvent = AnalyticsEvent.CreateSearchEvent(searchTerm, userId, sessionId, resultCount);
            await RecordEventAsync(analyticsEvent, cancellationToken);

            if (_options.LogToConsole)
            {
                _logger.LogInformation(
                    "Analytics: Search event - Term: {SearchTerm}, Results: {ResultCount}, User: {UserId}, Session: {SessionId}",
                    searchTerm, resultCount, userId, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track search event for term: {SearchTerm}", searchTerm);
        }
    }

    /// <inheritdoc />
    public async Task TrackProductViewAsync(
        Guid productId,
        Guid? sellerId,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (!ValidateIdentity(userId, sessionId))
        {
            return;
        }

        try
        {
            var analyticsEvent = AnalyticsEvent.CreateProductViewEvent(productId, sellerId, userId, sessionId);
            await RecordEventAsync(analyticsEvent, cancellationToken);

            if (_options.LogToConsole)
            {
                _logger.LogInformation(
                    "Analytics: ProductView event - Product: {ProductId}, Seller: {SellerId}, User: {UserId}, Session: {SessionId}",
                    productId, sellerId, userId, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track product view event for product: {ProductId}", productId);
        }
    }

    /// <inheritdoc />
    public async Task TrackAddToCartAsync(
        Guid productId,
        Guid? sellerId,
        int quantity,
        decimal unitPrice,
        string currency,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (!ValidateIdentity(userId, sessionId))
        {
            return;
        }

        try
        {
            var analyticsEvent = AnalyticsEvent.CreateAddToCartEvent(
                productId, sellerId, quantity, unitPrice, currency, userId, sessionId);
            await RecordEventAsync(analyticsEvent, cancellationToken);

            if (_options.LogToConsole)
            {
                _logger.LogInformation(
                    "Analytics: AddToCart event - Product: {ProductId}, Qty: {Quantity}, Value: {Currency} {Value}, User: {UserId}, Session: {SessionId}",
                    productId, quantity, currency, unitPrice * quantity, userId, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track add-to-cart event for product: {ProductId}", productId);
        }
    }

    /// <inheritdoc />
    public async Task TrackCheckoutStartAsync(
        decimal cartValue,
        string currency,
        int itemCount,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (!ValidateIdentity(userId, sessionId))
        {
            return;
        }

        try
        {
            var analyticsEvent = AnalyticsEvent.CreateCheckoutStartEvent(
                cartValue, currency, itemCount, userId, sessionId);
            await RecordEventAsync(analyticsEvent, cancellationToken);

            if (_options.LogToConsole)
            {
                _logger.LogInformation(
                    "Analytics: CheckoutStart event - Value: {Currency} {CartValue}, Items: {ItemCount}, User: {UserId}, Session: {SessionId}",
                    currency, cartValue, itemCount, userId, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track checkout start event");
        }
    }

    /// <inheritdoc />
    public async Task TrackOrderCompleteAsync(
        Guid orderId,
        decimal orderTotal,
        string currency,
        int itemCount,
        Guid? userId,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (!ValidateIdentity(userId, sessionId))
        {
            return;
        }

        try
        {
            var analyticsEvent = AnalyticsEvent.CreateOrderCompleteEvent(
                orderId, orderTotal, currency, itemCount, userId, sessionId);
            await RecordEventAsync(analyticsEvent, cancellationToken);

            if (_options.LogToConsole)
            {
                _logger.LogInformation(
                    "Analytics: OrderComplete event - Order: {OrderId}, Value: {Currency} {OrderTotal}, Items: {ItemCount}, User: {UserId}, Session: {SessionId}",
                    orderId, currency, orderTotal, itemCount, userId, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track order complete event for order: {OrderId}", orderId);
        }
    }

    private async Task RecordEventAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken)
    {
        if (!_options.PersistToDatabase)
        {
            return;
        }

        await _repository.AddAsync(analyticsEvent, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private bool ValidateIdentity(Guid? userId, string? sessionId)
    {
        if (userId is null && string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogDebug("Analytics: Skipping event - no user or session identity provided");
            return false;
        }
        return true;
    }
}
