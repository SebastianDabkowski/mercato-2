namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the public visibility status of a store.
/// </summary>
public enum StoreStatus
{
    /// <summary>
    /// Store is not yet verified and is not publicly visible.
    /// </summary>
    PendingVerification = 0,

    /// <summary>
    /// Store is fully active and publicly visible.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Store has limited activity but is still publicly visible.
    /// </summary>
    LimitedActive = 2,

    /// <summary>
    /// Store is suspended and not publicly visible.
    /// </summary>
    Suspended = 3
}
