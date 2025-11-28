using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to invite a new internal user to a store.
/// </summary>
public sealed record InviteInternalUserCommand(
    Guid StoreId,
    string Email,
    InternalUserRole Role,
    Guid InvitedByUserId);
