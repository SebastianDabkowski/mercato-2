using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Orchestrates shipping provider operations for creating shipments and processing tracking updates.
/// </summary>
public sealed class ShippingProviderIntegrationService
{
    private readonly IShippingProviderService _shippingProviderService;
    private readonly IShippingProviderRepository _shippingProviderRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IShipmentStatusHistoryRepository _shipmentStatusHistoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ShippingProviderIntegrationService> _logger;

    public ShippingProviderIntegrationService(
        IShippingProviderService shippingProviderService,
        IShippingProviderRepository shippingProviderRepository,
        IOrderRepository orderRepository,
        IShipmentStatusHistoryRepository shipmentStatusHistoryRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<ShippingProviderIntegrationService> logger)
    {
        _shippingProviderService = shippingProviderService;
        _shippingProviderRepository = shippingProviderRepository;
        _orderRepository = orderRepository;
        _shipmentStatusHistoryRepository = shipmentStatusHistoryRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a shipment via the specified provider and updates the order shipment with tracking details.
    /// </summary>
    public async Task<CreateProviderShipmentResult> CreateShipmentViaProviderAsync(
        Guid orderId,
        Guid shipmentId,
        Guid shippingProviderId,
        ShippingAddress senderAddress,
        PackageInfo packageInfo,
        string? serviceCode = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating shipment via provider for order {OrderId}, shipment {ShipmentId}, provider {ProviderId}",
            orderId, shipmentId, shippingProviderId);

        // Get the order with shipments
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
            return new CreateProviderShipmentResult(
                false, null, null, null, null, "Order not found.", "ORDER_NOT_FOUND");
        }

        // Find the shipment
        var shipment = order.Shipments.FirstOrDefault(s => s.Id == shipmentId);
        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found in order {OrderId}", shipmentId, orderId);
            return new CreateProviderShipmentResult(
                false, null, null, null, null, "Shipment not found.", "SHIPMENT_NOT_FOUND");
        }

        // Verify the shipment is in a state that allows shipping
        if (shipment.Status != ShipmentStatus.Paid && shipment.Status != ShipmentStatus.Processing)
        {
            _logger.LogWarning(
                "Shipment {ShipmentId} is in status {Status}, cannot create provider shipment",
                shipmentId, shipment.Status);
            return new CreateProviderShipmentResult(
                false, null, null, null, null,
                $"Cannot create shipment for order in status {shipment.Status}.",
                "INVALID_SHIPMENT_STATUS");
        }

        // Get the shipping provider configuration
        var provider = await _shippingProviderRepository.GetByIdAsync(shippingProviderId, cancellationToken);
        if (provider == null)
        {
            _logger.LogWarning("Shipping provider {ProviderId} not found", shippingProviderId);
            return new CreateProviderShipmentResult(
                false, null, null, null, null, "Shipping provider not found.", "PROVIDER_NOT_FOUND");
        }

        if (!provider.IsEnabled)
        {
            _logger.LogWarning("Shipping provider {ProviderId} is not enabled", shippingProviderId);
            return new CreateProviderShipmentResult(
                false, null, null, null, null, "Shipping provider is not enabled.", "PROVIDER_DISABLED");
        }

        // Build recipient address from order
        var recipientAddress = new ShippingAddress(
            order.RecipientName,
            order.DeliveryStreet,
            order.DeliveryStreet2,
            order.DeliveryCity,
            order.DeliveryState,
            order.DeliveryPostalCode,
            order.DeliveryCountry,
            order.DeliveryPhoneNumber,
            null);

        // Call the shipping provider to create the shipment
        var result = await _shippingProviderService.CreateShipmentAsync(
            shippingProviderId,
            shipmentId,
            senderAddress,
            recipientAddress,
            packageInfo,
            serviceCode,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Failed to create shipment via provider: {ErrorMessage} ({ErrorCode})",
                result.ErrorMessage, result.ErrorCode);
            return new CreateProviderShipmentResult(
                false, null, null, null, null, result.ErrorMessage, result.ErrorCode);
        }

        // Store previous status for history
        var previousStatus = shipment.Status;

        // Update the shipment with provider details
        var carrierName = _shippingProviderService.GetCarrierName(provider.ProviderType);
        shipment.ShipViaProvider(
            shippingProviderId,
            result.ProviderShipmentId!,
            carrierName,
            result.TrackingNumber,
            result.TrackingUrl,
            result.LabelUrl);

        // Save the changes
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Record status history
        var history = new ShipmentStatusHistory(
            shipmentId,
            orderId,
            previousStatus,
            ShipmentStatus.Shipped,
            userId,
            userId.HasValue ? StatusChangeActorType.Seller : StatusChangeActorType.System,
            carrierName,
            result.TrackingNumber,
            result.TrackingUrl,
            $"Shipment created via {provider.Name} provider.");

        await _shipmentStatusHistoryRepository.AddAsync(history, cancellationToken);
        await _shipmentStatusHistoryRepository.SaveChangesAsync(cancellationToken);

        // Send notification to buyer
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer != null)
        {
            await _notificationService.SendShipmentStatusChangedAsync(
                shipmentId,
                orderId,
                buyer.Email.Value,
                order.OrderNumber,
                previousStatus.ToString(),
                ShipmentStatus.Shipped.ToString(),
                result.TrackingNumber,
                carrierName,
                cancellationToken);
        }

        _logger.LogInformation(
            "Successfully created shipment via provider. Provider shipment ID: {ProviderShipmentId}, Tracking: {TrackingNumber}",
            result.ProviderShipmentId, result.TrackingNumber);

        return new CreateProviderShipmentResult(
            true,
            result.ProviderShipmentId,
            result.TrackingNumber,
            result.TrackingUrl,
            result.LabelUrl,
            null,
            null);
    }

    /// <summary>
    /// Processes tracking updates from a shipping provider and updates shipment status accordingly.
    /// </summary>
    public async Task<ProcessTrackingResult> ProcessTrackingUpdatesAsync(
        Guid orderId,
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing tracking updates for order {OrderId}, shipment {ShipmentId}",
            orderId, shipmentId);

        // Get the order with shipments
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            return new ProcessTrackingResult(false, null, "Order not found.", "ORDER_NOT_FOUND");
        }

        // Find the shipment
        var shipment = order.Shipments.FirstOrDefault(s => s.Id == shipmentId);
        if (shipment == null)
        {
            return new ProcessTrackingResult(false, null, "Shipment not found.", "SHIPMENT_NOT_FOUND");
        }

        // Check if this is a provider-tracked shipment
        if (!shipment.ShippingProviderId.HasValue || string.IsNullOrEmpty(shipment.ProviderShipmentId))
        {
            return new ProcessTrackingResult(false, null, "Shipment is not tracked via provider.", "NOT_PROVIDER_TRACKED");
        }

        // Get tracking updates from the provider
        var result = await _shippingProviderService.GetTrackingUpdatesAsync(
            shipment.ShippingProviderId.Value,
            shipment.ProviderShipmentId,
            cancellationToken);

        if (!result.IsSuccess || result.Updates == null || result.Updates.Count == 0)
        {
            return new ProcessTrackingResult(
                false, null, result.ErrorMessage ?? "No tracking updates available.", result.ErrorCode);
        }

        // Get the latest update
        var latestUpdate = result.Updates
            .OrderByDescending(u => u.Timestamp)
            .First();

        var previousStatus = shipment.Status;
        var statusChanged = false;

        // Update the provider status
        shipment.UpdateProviderStatus(latestUpdate.Status);

        // Check if delivered
        if (latestUpdate.IsDelivered && shipment.Status == ShipmentStatus.Shipped)
        {
            shipment.MarkDelivered();
            statusChanged = true;

            // Record status history
            var history = new ShipmentStatusHistory(
                shipmentId,
                orderId,
                previousStatus,
                ShipmentStatus.Delivered,
                null,
                StatusChangeActorType.System,
                shipment.CarrierName,
                shipment.TrackingNumber,
                shipment.TrackingUrl,
                $"Delivered: {latestUpdate.StatusDescription}");

            await _shipmentStatusHistoryRepository.AddAsync(history, cancellationToken);
        }

        // Save changes
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        await _shipmentStatusHistoryRepository.SaveChangesAsync(cancellationToken);

        // Send notification if delivered
        if (statusChanged && shipment.Status == ShipmentStatus.Delivered)
        {
            var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
            if (buyer != null)
            {
                await _notificationService.SendShipmentStatusChangedAsync(
                    shipmentId,
                    orderId,
                    buyer.Email.Value,
                    order.OrderNumber,
                    previousStatus.ToString(),
                    ShipmentStatus.Delivered.ToString(),
                    shipment.TrackingNumber,
                    shipment.CarrierName,
                    cancellationToken);
            }
        }

        _logger.LogInformation(
            "Processed tracking updates for shipment {ShipmentId}. Status: {Status}, Delivered: {IsDelivered}",
            shipmentId, latestUpdate.Status, latestUpdate.IsDelivered);

        return new ProcessTrackingResult(
            true,
            latestUpdate.Status,
            null,
            null);
    }

    /// <summary>
    /// Gets available shipping providers for a store.
    /// </summary>
    public async Task<IReadOnlyList<ShippingProviderInfo>> GetAvailableProvidersAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var providers = await _shippingProviderRepository.GetEnabledByStoreIdAsync(storeId, cancellationToken);
        var platformProviders = await _shippingProviderRepository.GetPlatformProvidersAsync(cancellationToken);

        var allProviders = providers
            .Concat(platformProviders.Where(p => p.IsEnabled))
            .Select(p => new ShippingProviderInfo(
                p.Id,
                p.ProviderType,
                p.Name,
                _shippingProviderService.GetCarrierName(p.ProviderType),
                p.IsEnabled && p.HasValidCredentials()))
            .ToList();

        return allProviders;
    }
}

/// <summary>
/// Result of creating a shipment via a provider.
/// </summary>
public sealed record CreateProviderShipmentResult(
    bool IsSuccess,
    string? ProviderShipmentId,
    string? TrackingNumber,
    string? TrackingUrl,
    string? LabelUrl,
    string? ErrorMessage,
    string? ErrorCode);

/// <summary>
/// Result of processing tracking updates.
/// </summary>
public sealed record ProcessTrackingResult(
    bool IsSuccess,
    string? LatestStatus,
    string? ErrorMessage,
    string? ErrorCode);

/// <summary>
/// Information about an available shipping provider.
/// </summary>
public sealed record ShippingProviderInfo(
    Guid Id,
    ShippingProviderType ProviderType,
    string Name,
    string CarrierName,
    bool IsReady);
