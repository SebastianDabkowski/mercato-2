namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a store by its ID.
/// </summary>
/// <param name="StoreId">The store's unique identifier.</param>
/// <param name="PublicOnly">When true, only returns publicly visible stores (Active or LimitedActive).</param>
public sealed record GetStoreByIdQuery(Guid StoreId, bool PublicOnly = false);
