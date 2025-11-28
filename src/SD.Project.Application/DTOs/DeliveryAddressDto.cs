namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a delivery address.
/// </summary>
public sealed record DeliveryAddressDto(
    Guid Id,
    string RecipientName,
    string? PhoneNumber,
    string? Label,
    string Street,
    string? Street2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Result of saving a delivery address.
/// </summary>
public sealed record SaveDeliveryAddressResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    DeliveryAddressDto? Address)
{
    public static SaveDeliveryAddressResultDto Succeeded(DeliveryAddressDto address)
        => new(true, null, address);

    public static SaveDeliveryAddressResultDto Failed(string errorMessage)
        => new(false, errorMessage, null);
}

/// <summary>
/// Result of validating shipping availability to an address.
/// </summary>
public sealed record ValidateShippingResultDto(
    bool CanShip,
    string? Message,
    IReadOnlyList<string> RestrictedProductNames)
{
    public static ValidateShippingResultDto Success()
        => new(true, null, Array.Empty<string>());

    public static ValidateShippingResultDto RegionRestricted(string message, IReadOnlyList<string> restrictedProducts)
        => new(false, message, restrictedProducts);
}

/// <summary>
/// Result of setting a default address.
/// </summary>
public sealed record SetDefaultAddressResultDto(
    bool IsSuccess,
    string? ErrorMessage)
{
    public static SetDefaultAddressResultDto Succeeded()
        => new(true, null);

    public static SetDefaultAddressResultDto Failed(string errorMessage)
        => new(false, errorMessage);
}

/// <summary>
/// Result of deleting an address.
/// </summary>
public sealed record DeleteDeliveryAddressResultDto(
    bool IsSuccess,
    string? ErrorMessage)
{
    public static DeleteDeliveryAddressResultDto Succeeded()
        => new(true, null);

    public static DeleteDeliveryAddressResultDto Failed(string errorMessage)
        => new(false, errorMessage);
}
