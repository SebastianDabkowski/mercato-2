namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a store by its ID.
/// </summary>
public sealed record GetStoreByIdQuery(Guid StoreId);
