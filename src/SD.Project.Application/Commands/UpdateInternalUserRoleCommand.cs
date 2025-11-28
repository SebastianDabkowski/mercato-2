using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to update an internal user's role.
/// </summary>
public sealed record UpdateInternalUserRoleCommand(
    Guid InternalUserId,
    InternalUserRole NewRole,
    Guid RequestedByUserId);
