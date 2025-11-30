namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new shipping method for a store.
/// </summary>
public sealed record CreateShippingMethodCommand(
    Guid StoreId,
    string Name,
    string? Description,
    string? CarrierName,
    int EstimatedDeliveryDaysMin,
    int EstimatedDeliveryDaysMax,
    decimal BaseCost,
    decimal CostPerItem,
    decimal? FreeShippingThreshold,
    string Currency,
    int DisplayOrder,
    string? AvailableRegions,
    bool IsDefault);

/// <summary>
/// Command to update an existing shipping method.
/// </summary>
public sealed record UpdateShippingMethodCommand(
    Guid ShippingMethodId,
    Guid StoreId,
    string Name,
    string? Description,
    string? CarrierName,
    int EstimatedDeliveryDaysMin,
    int EstimatedDeliveryDaysMax,
    decimal BaseCost,
    decimal CostPerItem,
    decimal? FreeShippingThreshold,
    int DisplayOrder,
    string? AvailableRegions);

/// <summary>
/// Command to toggle the active status of a shipping method.
/// </summary>
public sealed record ToggleShippingMethodStatusCommand(
    Guid ShippingMethodId,
    Guid StoreId,
    bool IsActive);

/// <summary>
/// Command to delete a shipping method.
/// </summary>
public sealed record DeleteShippingMethodCommand(
    Guid ShippingMethodId,
    Guid StoreId);
