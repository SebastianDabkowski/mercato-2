namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all saved addresses for a buyer.
/// </summary>
public sealed record GetDeliveryAddressesQuery(
    Guid? BuyerId,
    string? SessionId);

/// <summary>
/// Query to get a specific address by ID.
/// </summary>
public sealed record GetDeliveryAddressByIdQuery(
    Guid? BuyerId,
    string? SessionId,
    Guid AddressId);

/// <summary>
/// Query to get the default address for a buyer.
/// </summary>
public sealed record GetDefaultDeliveryAddressQuery(
    Guid BuyerId);
