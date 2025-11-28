namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get import job history for a store.
/// </summary>
public sealed record GetImportJobsByStoreIdQuery(Guid StoreId);

/// <summary>
/// Query to get a specific import job by ID.
/// </summary>
public sealed record GetImportJobByIdQuery(Guid ImportJobId);
