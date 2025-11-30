namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a shipping method for settings/management purposes.
/// </summary>
public sealed record ShippingMethodSettingsDto(
    Guid Id,
    Guid? StoreId,
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
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>
    /// Gets a formatted delivery time string.
    /// </summary>
    public string DeliveryTimeDisplay
    {
        get
        {
            if (EstimatedDeliveryDaysMin == EstimatedDeliveryDaysMax)
            {
                return EstimatedDeliveryDaysMin == 1
                    ? "1 business day"
                    : $"{EstimatedDeliveryDaysMin} business days";
            }

            return $"{EstimatedDeliveryDaysMin}-{EstimatedDeliveryDaysMax} business days";
        }
    }
}

/// <summary>
/// DTO representing the result of a shipping method operation.
/// </summary>
public sealed record ShippingMethodSettingsResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public ShippingMethodSettingsDto? ShippingMethod { get; init; }

    public static ShippingMethodSettingsResultDto Succeeded(ShippingMethodSettingsDto shippingMethod, string message)
    {
        return new ShippingMethodSettingsResultDto
        {
            Success = true,
            Message = message,
            ShippingMethod = shippingMethod
        };
    }

    public static ShippingMethodSettingsResultDto Succeeded(string message)
    {
        return new ShippingMethodSettingsResultDto
        {
            Success = true,
            Message = message
        };
    }

    public static ShippingMethodSettingsResultDto Failed(string error)
    {
        return new ShippingMethodSettingsResultDto
        {
            Success = false,
            Errors = new[] { error }
        };
    }

    public static ShippingMethodSettingsResultDto Failed(IReadOnlyList<string> errors)
    {
        return new ShippingMethodSettingsResultDto
        {
            Success = false,
            Errors = errors
        };
    }
}
