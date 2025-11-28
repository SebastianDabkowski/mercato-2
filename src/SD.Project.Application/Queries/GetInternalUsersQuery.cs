namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all internal users for a store.
/// </summary>
public sealed record GetInternalUsersQuery(Guid StoreId);
