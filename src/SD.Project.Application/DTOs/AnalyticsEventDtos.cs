using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing an analytics event for API responses.
/// </summary>
/// <param name="Id">Unique event identifier.</param>
/// <param name="Timestamp">UTC timestamp of the event.</param>
/// <param name="EventType">Type of the event.</param>
/// <param name="UserId">Authenticated user ID if available.</param>
/// <param name="SessionId">Session identifier.</param>
/// <param name="ProductId">Product ID if applicable.</param>
/// <param name="SellerId">Seller/Store ID if applicable.</param>
/// <param name="OrderId">Order ID if applicable.</param>
/// <param name="SearchTerm">Search term for search events.</param>
/// <param name="Quantity">Quantity for cart/order events.</param>
/// <param name="Value">Monetary value if applicable.</param>
/// <param name="Currency">Currency code if applicable.</param>
public record AnalyticsEventDto(
    Guid Id,
    DateTime Timestamp,
    string EventType,
    Guid? UserId,
    string? SessionId,
    Guid? ProductId,
    Guid? SellerId,
    Guid? OrderId,
    string? SearchTerm,
    int? Quantity,
    decimal? Value,
    string? Currency);

/// <summary>
/// Summary of event counts by type for a time period.
/// </summary>
/// <param name="SearchCount">Number of search events.</param>
/// <param name="ProductViewCount">Number of product view events.</param>
/// <param name="AddToCartCount">Number of add-to-cart events.</param>
/// <param name="CheckoutStartCount">Number of checkout start events.</param>
/// <param name="OrderCompleteCount">Number of order completion events.</param>
/// <param name="FromDate">Start of the reporting period.</param>
/// <param name="ToDate">End of the reporting period.</param>
public record AnalyticsEventSummaryDto(
    int SearchCount,
    int ProductViewCount,
    int AddToCartCount,
    int CheckoutStartCount,
    int OrderCompleteCount,
    DateTime FromDate,
    DateTime ToDate);

/// <summary>
/// Paged result of analytics events for querying.
/// </summary>
/// <param name="Events">The events in the current page.</param>
/// <param name="TotalCount">Total number of events matching the filter.</param>
/// <param name="PageNumber">Current page number.</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="TotalPages">Total number of pages.</param>
public record AnalyticsEventsPagedResultDto(
    IReadOnlyList<AnalyticsEventDto> Events,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
