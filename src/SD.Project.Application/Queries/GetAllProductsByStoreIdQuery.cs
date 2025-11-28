namespace SD.Project.Application.Queries;

/// <summary>
/// Query for retrieving all products associated with a store (including drafts).
/// </summary>
public sealed record GetAllProductsByStoreIdQuery(Guid StoreId);
