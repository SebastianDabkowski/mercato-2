using Microsoft.Extensions.Logging;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for shipping label operations.
/// Handles label generation, storage, download, and voiding.
/// </summary>
public sealed class ShippingLabelService
{
    private readonly IShippingLabelRepository _labelRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IShippingProviderRepository _providerRepository;
    private readonly IShippingProviderService _providerService;
    private readonly IShippingLabelStorageService _storageService;
    private readonly ILogger<ShippingLabelService> _logger;

    public ShippingLabelService(
        IShippingLabelRepository labelRepository,
        IOrderRepository orderRepository,
        IShippingProviderRepository providerRepository,
        IShippingProviderService providerService,
        IShippingLabelStorageService storageService,
        ILogger<ShippingLabelService> logger)
    {
        _labelRepository = labelRepository;
        _orderRepository = orderRepository;
        _providerRepository = providerRepository;
        _providerService = providerService;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a shipping label for a shipment.
    /// </summary>
    public async Task<GenerateLabelResultDto> HandleAsync(
        GenerateShippingLabelCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(
            "Generating shipping label for shipment {ShipmentId} by user {UserId}",
            command.ShipmentId, command.GeneratedByUserId);

        // Get the shipment with order
        var (shipment, order, _) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return new GenerateLabelResultDto(false, null, "Shipment not found.");
        }

        // Verify the shipment belongs to the requested store
        if (shipment.StoreId != command.StoreId)
        {
            return new GenerateLabelResultDto(false, null, "Shipment does not belong to this store.");
        }

        // Check if shipment has provider integration
        if (!shipment.ShippingProviderId.HasValue || string.IsNullOrEmpty(shipment.ProviderShipmentId))
        {
            return new GenerateLabelResultDto(
                false, null, 
                "Cannot generate label: Shipment was not created via a shipping provider. Labels can only be generated for shipments with provider integration.");
        }

        // Check if a valid label already exists
        var existingLabel = await _labelRepository.GetByShipmentIdAsync(command.ShipmentId, cancellationToken);
        if (existingLabel is not null && existingLabel.IsValid())
        {
            _logger.LogInformation(
                "Valid label already exists for shipment {ShipmentId}",
                command.ShipmentId);
            return new GenerateLabelResultDto(true, MapToDto(existingLabel), null);
        }

        // Get the shipping provider
        var provider = await _providerRepository.GetByIdAsync(
            shipment.ShippingProviderId.Value, 
            cancellationToken);

        if (provider is null)
        {
            return new GenerateLabelResultDto(false, null, "Shipping provider not found.");
        }

        if (!_providerService.SupportsLabelGeneration(provider.ProviderType))
        {
            return new GenerateLabelResultDto(
                false, null, 
                "This shipping provider does not support label generation.");
        }

        // Generate label from provider
        var options = new LabelOptions(command.Format, command.LabelSize);
        var result = await _providerService.GenerateLabelAsync(
            shipment.ShippingProviderId.Value,
            shipment.ProviderShipmentId,
            options,
            cancellationToken);

        if (!result.IsSuccess || result.LabelData is null)
        {
            _logger.LogWarning(
                "Failed to generate label from provider: {Error} ({ErrorCode})",
                result.ErrorMessage, result.ErrorCode);
            return new GenerateLabelResultDto(false, null, result.ErrorMessage ?? "Failed to generate label.");
        }

        // Store the label
        var storeResult = await _storageService.StoreLabelAsync(
            command.ShipmentId,
            result.LabelData,
            result.Format ?? "PDF",
            cancellationToken);

        if (!storeResult.IsSuccess || string.IsNullOrEmpty(storeResult.StoragePath))
        {
            _logger.LogError(
                "Failed to store shipping label: {Error}",
                storeResult.ErrorMessage);
            return new GenerateLabelResultDto(false, null, "Failed to store shipping label.");
        }

        // Create label entity
        var label = new ShippingLabel(
            shipment.Id,
            order.Id,
            shipment.ShippingProviderId.Value,
            result.Format ?? "PDF",
            storeResult.StoragePath,
            storeResult.ContentType ?? "application/pdf",
            storeResult.FileSizeBytes,
            result.ProviderLabelId,
            result.LabelSize,
            result.TrackingNumber ?? shipment.TrackingNumber,
            result.CarrierName ?? shipment.CarrierName,
            result.ExternalUrl,
            result.ExpiresAt);

        await _labelRepository.AddAsync(label, cancellationToken);
        await _labelRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully generated shipping label {LabelId} for shipment {ShipmentId}",
            label.Id, command.ShipmentId);

        return new GenerateLabelResultDto(true, MapToDto(label), null);
    }

    /// <summary>
    /// Gets a shipping label by ID.
    /// </summary>
    public async Task<ShippingLabelDto?> HandleAsync(
        GetShippingLabelQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var label = await _labelRepository.GetByIdAsync(query.LabelId, cancellationToken);
        if (label is null)
        {
            return null;
        }

        // Verify the label belongs to the store
        var (shipment, _, _) = await _orderRepository.GetShipmentWithOrderAsync(
            label.ShipmentId,
            cancellationToken);

        if (shipment is null || shipment.StoreId != query.StoreId)
        {
            return null;
        }

        return MapToDto(label);
    }

    /// <summary>
    /// Gets the active shipping label for a shipment.
    /// </summary>
    public async Task<ShippingLabelDto?> HandleAsync(
        GetShippingLabelByShipmentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Verify the shipment belongs to the store
        var (shipment, _, _) = await _orderRepository.GetShipmentWithOrderAsync(
            query.ShipmentId,
            cancellationToken);

        if (shipment is null || shipment.StoreId != query.StoreId)
        {
            return null;
        }

        var label = await _labelRepository.GetByShipmentIdAsync(query.ShipmentId, cancellationToken);
        return label is not null ? MapToDto(label) : null;
    }

    /// <summary>
    /// Downloads a shipping label.
    /// </summary>
    public async Task<DownloadLabelResultDto> HandleAsync(
        DownloadShippingLabelQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var label = await _labelRepository.GetByIdAsync(query.LabelId, cancellationToken);
        if (label is null)
        {
            return new DownloadLabelResultDto(false, null, null, null, "Label not found.");
        }

        // Verify the label belongs to the store
        var (shipment, _, _) = await _orderRepository.GetShipmentWithOrderAsync(
            label.ShipmentId,
            cancellationToken);

        if (shipment is null || shipment.StoreId != query.StoreId)
        {
            return new DownloadLabelResultDto(false, null, null, null, "Label not found.");
        }

        if (!label.IsValid())
        {
            var reason = label.IsVoided ? "Label has been voided." : "Label has expired.";
            return new DownloadLabelResultDto(false, null, null, null, reason);
        }

        // Retrieve from storage
        var result = await _storageService.RetrieveLabelAsync(label.StoragePath, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
        {
            return new DownloadLabelResultDto(false, null, null, null, result.ErrorMessage ?? "Failed to retrieve label.");
        }

        // Record access
        label.RecordAccess();
        await _labelRepository.UpdateAsync(label, cancellationToken);
        await _labelRepository.SaveChangesAsync(cancellationToken);

        var fileName = $"shipping-label-{label.TrackingNumber ?? label.Id.ToString()}.{label.Format.ToLowerInvariant()}";

        _logger.LogInformation(
            "Label {LabelId} downloaded (access count: {AccessCount})",
            label.Id, label.AccessCount);

        return new DownloadLabelResultDto(true, result.Data, label.ContentType, fileName, null);
    }

    /// <summary>
    /// Voids a shipping label.
    /// </summary>
    public async Task<VoidLabelResultDto> HandleAsync(
        VoidShippingLabelCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var label = await _labelRepository.GetByIdAsync(command.LabelId, cancellationToken);
        if (label is null)
        {
            return new VoidLabelResultDto(false, "Label not found.");
        }

        // Verify the label belongs to the store
        var (shipment, _, _) = await _orderRepository.GetShipmentWithOrderAsync(
            label.ShipmentId,
            cancellationToken);

        if (shipment is null || shipment.StoreId != command.StoreId)
        {
            return new VoidLabelResultDto(false, "Label not found.");
        }

        if (label.IsVoided)
        {
            return new VoidLabelResultDto(false, "Label is already voided.");
        }

        try
        {
            label.Void(command.Reason);
            await _labelRepository.UpdateAsync(label, cancellationToken);
            await _labelRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Label {LabelId} voided by user {UserId}. Reason: {Reason}",
                label.Id, command.VoidedByUserId, command.Reason ?? "Not specified");

            return new VoidLabelResultDto(true, null);
        }
        catch (InvalidOperationException ex)
        {
            return new VoidLabelResultDto(false, ex.Message);
        }
    }

    private static ShippingLabelDto MapToDto(ShippingLabel label)
    {
        return new ShippingLabelDto(
            label.Id,
            label.ShipmentId,
            label.OrderId,
            label.Format,
            label.LabelSize,
            label.TrackingNumber,
            label.CarrierName,
            label.ContentType,
            label.FileSizeBytes,
            label.GeneratedAt,
            label.ExpiresAt,
            label.IsValid(),
            label.IsVoided,
            label.AccessCount);
    }
}
