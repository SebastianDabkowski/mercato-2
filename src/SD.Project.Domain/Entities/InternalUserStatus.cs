namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the status of an internal user within a store.
/// </summary>
public enum InternalUserStatus
{
    /// <summary>
    /// The user has been invited but has not yet accepted the invitation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The user is active and can access the seller panel with their assigned role.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The user has been deactivated and cannot sign in to the seller panel.
    /// </summary>
    Deactivated = 2
}
