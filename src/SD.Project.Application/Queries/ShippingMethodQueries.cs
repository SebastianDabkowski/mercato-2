namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all shipping methods for a store (including inactive) for settings management.
/// </summary>
public sealed record GetShippingMethodsByStoreIdQuery(Guid StoreId);

/// <summary>
/// Query to get a shipping method by ID.
/// </summary>
public sealed record GetShippingMethodByIdQuery(Guid ShippingMethodId);
