using SD.Project.Domain.Entities;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Represents the result of creating a shipment via a shipping provider.
/// </summary>
public sealed record CreateShipmentResult(
    bool IsSuccess,
    string? ProviderShipmentId,
    string? TrackingNumber,
    string? TrackingUrl,
    string? LabelUrl,
    string? ErrorMessage,
    string? ErrorCode);

/// <summary>
/// Represents a tracking status update from a shipping provider.
/// </summary>
public sealed record TrackingStatusUpdate(
    string ProviderShipmentId,
    string Status,
    string? StatusDescription,
    DateTime Timestamp,
    string? Location,
    bool IsDelivered);

/// <summary>
/// Represents the result of fetching tracking updates from a shipping provider.
/// </summary>
public sealed record TrackingUpdateResult(
    bool IsSuccess,
    IReadOnlyList<TrackingStatusUpdate>? Updates,
    string? ErrorMessage,
    string? ErrorCode);

/// <summary>
/// Represents the result of getting available shipping rates from a provider.
/// </summary>
public sealed record ShippingRateResult(
    bool IsSuccess,
    IReadOnlyList<ShippingRate>? Rates,
    string? ErrorMessage,
    string? ErrorCode);

/// <summary>
/// Represents a shipping rate option from a provider.
/// </summary>
public sealed record ShippingRate(
    string ServiceCode,
    string ServiceName,
    decimal Cost,
    string Currency,
    int? EstimatedDeliveryDays,
    DateTime? EstimatedDeliveryDate);

/// <summary>
/// Represents shipping address information for provider API calls.
/// </summary>
public sealed record ShippingAddress(
    string RecipientName,
    string Street,
    string? Street2,
    string City,
    string? State,
    string PostalCode,
    string CountryCode,
    string? PhoneNumber,
    string? Email);

/// <summary>
/// Represents package dimensions and weight for provider API calls.
/// </summary>
public sealed record PackageInfo(
    decimal WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm,
    string? PackageType);

/// <summary>
/// Abstraction for integrating with shipping providers (e.g., DHL, UPS, FedEx, InPost).
/// Provides methods for creating shipments, fetching tracking updates, and getting rates.
/// </summary>
public interface IShippingProviderService
{
    /// <summary>
    /// Creates a shipment with the specified provider.
    /// </summary>
    /// <param name="providerId">The shipping provider configuration ID.</param>
    /// <param name="shipmentId">The internal shipment ID for reference.</param>
    /// <param name="senderAddress">The sender (seller) address.</param>
    /// <param name="recipientAddress">The recipient (buyer) address.</param>
    /// <param name="packageInfo">Package dimensions and weight.</param>
    /// <param name="serviceCode">Optional service code for specific shipping service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing tracking information or error details.</returns>
    Task<CreateShipmentResult> CreateShipmentAsync(
        Guid providerId,
        Guid shipmentId,
        ShippingAddress senderAddress,
        ShippingAddress recipientAddress,
        PackageInfo packageInfo,
        string? serviceCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches tracking updates for a shipment from the provider.
    /// </summary>
    /// <param name="providerId">The shipping provider configuration ID.</param>
    /// <param name="providerShipmentId">The provider's shipment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing tracking updates or error details.</returns>
    Task<TrackingUpdateResult> GetTrackingUpdatesAsync(
        Guid providerId,
        string providerShipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available shipping rates from a provider for the given addresses.
    /// </summary>
    /// <param name="providerId">The shipping provider configuration ID.</param>
    /// <param name="senderAddress">The sender (seller) address.</param>
    /// <param name="recipientAddress">The recipient (buyer) address.</param>
    /// <param name="packageInfo">Package dimensions and weight.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing available rates or error details.</returns>
    Task<ShippingRateResult> GetRatesAsync(
        Guid providerId,
        ShippingAddress senderAddress,
        ShippingAddress recipientAddress,
        PackageInfo packageInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a provider is configured and ready to use.
    /// </summary>
    /// <param name="providerId">The shipping provider configuration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider is ready, false otherwise.</returns>
    Task<bool> IsProviderReadyAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the carrier name for a provider type.
    /// </summary>
    /// <param name="providerType">The provider type.</param>
    /// <returns>The carrier name.</returns>
    string GetCarrierName(ShippingProviderType providerType);
}
