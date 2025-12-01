using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a paginated and filtered list of users for admin management.
/// </summary>
public sealed record GetUsersForAdminQuery(
    UserRole? RoleFilter,
    UserStatus? StatusFilter,
    string? SearchTerm,
    int PageNumber,
    int PageSize);

/// <summary>
/// Query to get detailed information about a specific user.
/// </summary>
public sealed record GetUserDetailQuery(Guid UserId);
