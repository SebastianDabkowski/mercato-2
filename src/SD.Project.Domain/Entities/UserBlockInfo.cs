namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the blocking details for a user account.
/// This entity serves as an audit log for user blocking actions.
/// </summary>
public class UserBlockInfo
{
    /// <summary>
    /// The unique identifier of the block record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user who was blocked.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The ID of the admin who blocked the user.
    /// </summary>
    public Guid BlockedByAdminId { get; private set; }

    /// <summary>
    /// The reason for blocking the user.
    /// </summary>
    public BlockReason Reason { get; private set; }

    /// <summary>
    /// Optional detailed notes about the blocking decision.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// The UTC timestamp when the user was blocked.
    /// </summary>
    public DateTime BlockedAt { get; private set; }

    /// <summary>
    /// Indicates whether this block is currently active.
    /// False if the user has been unblocked.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The ID of the admin who unblocked the user, if applicable.
    /// </summary>
    public Guid? UnblockedByAdminId { get; private set; }

    /// <summary>
    /// The UTC timestamp when the user was unblocked, if applicable.
    /// </summary>
    public DateTime? UnblockedAt { get; private set; }

    /// <summary>
    /// Optional notes explaining why the user was reactivated/unblocked.
    /// </summary>
    public string? ReactivationNotes { get; private set; }

    private UserBlockInfo()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new block record for a user.
    /// </summary>
    /// <param name="userId">The ID of the user being blocked.</param>
    /// <param name="blockedByAdminId">The ID of the admin performing the block.</param>
    /// <param name="reason">The reason for blocking.</param>
    /// <param name="notes">Optional detailed notes.</param>
    public UserBlockInfo(
        Guid userId,
        Guid blockedByAdminId,
        BlockReason reason,
        string? notes = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (blockedByAdminId == Guid.Empty)
        {
            throw new ArgumentException("Admin ID is required.", nameof(blockedByAdminId));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        BlockedByAdminId = blockedByAdminId;
        Reason = reason;
        Notes = notes?.Trim();
        BlockedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Marks this block as inactive when the user is unblocked/reactivated.
    /// </summary>
    /// <param name="unblockedByAdminId">The ID of the admin performing the unblock.</param>
    /// <param name="reactivationNotes">Optional notes explaining the reactivation.</param>
    public void Unblock(Guid unblockedByAdminId, string? reactivationNotes = null)
    {
        if (unblockedByAdminId == Guid.Empty)
        {
            throw new ArgumentException("Admin ID is required.", nameof(unblockedByAdminId));
        }

        if (!IsActive)
        {
            throw new InvalidOperationException("User is not currently blocked.");
        }

        IsActive = false;
        UnblockedByAdminId = unblockedByAdminId;
        UnblockedAt = DateTime.UtcNow;
        ReactivationNotes = reactivationNotes?.Trim();
    }
}
