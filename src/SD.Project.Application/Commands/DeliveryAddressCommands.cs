namespace SD.Project.Application.Commands;

/// <summary>
/// Command to save a new delivery address or update an existing one.
/// </summary>
public sealed record SaveDeliveryAddressCommand(
    Guid? BuyerId,
    string? SessionId,
    Guid? AddressId,  // Null for new address, set for update
    string RecipientName,
    string? PhoneNumber,
    string? Label,
    string Street,
    string? Street2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    bool SetAsDefault,
    bool SaveToProfile);  // For guest checkout: whether to save address when they create account

/// <summary>
/// Command to set an address as the default for a buyer.
/// </summary>
public sealed record SetDefaultAddressCommand(
    Guid BuyerId,
    Guid AddressId);

/// <summary>
/// Command to delete (deactivate) a delivery address.
/// </summary>
public sealed record DeleteDeliveryAddressCommand(
    Guid BuyerId,
    Guid AddressId);

/// <summary>
/// Command to validate if shipping is available to an address for the current cart.
/// </summary>
public sealed record ValidateShippingCommand(
    Guid? BuyerId,
    string? SessionId,
    string Country,
    string? State,
    string PostalCode);
