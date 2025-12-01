namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all consents for a user.
/// </summary>
public record GetUserConsentsQuery(Guid UserId);

/// <summary>
/// Query to get all consent types.
/// </summary>
public record GetConsentTypesQuery(bool IncludeInactive = false);

/// <summary>
/// Query to get active consent types (for presenting to users).
/// </summary>
public record GetActiveConsentTypesQuery();

/// <summary>
/// Query to check if a user has active consent for a specific type.
/// </summary>
public record CheckUserConsentQuery(Guid UserId, string ConsentTypeCode);

/// <summary>
/// Query to get audit logs for a user's consents.
/// </summary>
public record GetUserConsentAuditLogsQuery(Guid UserId);

/// <summary>
/// Query to get a consent type by ID.
/// </summary>
public record GetConsentTypeByIdQuery(Guid Id);

/// <summary>
/// Query to get consent versions for a consent type.
/// </summary>
public record GetConsentVersionsQuery(Guid ConsentTypeId);

/// <summary>
/// Query to get users with active consent for a specific type.
/// Used for bulk operations like marketing sends.
/// </summary>
public record GetUsersWithConsentQuery(string ConsentTypeCode);
