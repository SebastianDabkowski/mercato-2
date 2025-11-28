namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a store by seller ID.
/// </summary>
public sealed record GetStoreBySellerIdQuery(Guid SellerId);
