namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a specific internal user by ID.
/// </summary>
public sealed record GetInternalUserByIdQuery(Guid InternalUserId);
