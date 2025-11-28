namespace SD.Project.Application.Queries;

/// <summary>
/// Query for retrieving products associated with a store.
/// </summary>
public sealed record GetProductsByStoreIdQuery(Guid StoreId);
