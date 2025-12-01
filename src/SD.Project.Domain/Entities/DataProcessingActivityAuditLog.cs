namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an audit log entry for data processing activity changes.
/// Used to track who changed what and when for compliance purposes.
/// </summary>
public class DataProcessingActivityAuditLog
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the data processing activity that was modified.
    /// </summary>
    public Guid DataProcessingActivityId { get; private set; }

    /// <summary>
    /// The ID of the user who made the change.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The type of action performed (Created, Updated, Archived, Reactivated).
    /// </summary>
    public DataProcessingActivityAuditAction Action { get; private set; }

    /// <summary>
    /// JSON snapshot of the previous state (null for creation).
    /// </summary>
    public string? PreviousState { get; private set; }

    /// <summary>
    /// JSON snapshot of the new state.
    /// </summary>
    public string NewState { get; private set; } = default!;

    /// <summary>
    /// Optional change reason or comment provided by the user.
    /// </summary>
    public string? ChangeReason { get; private set; }

    /// <summary>
    /// The UTC timestamp when the change was made.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private DataProcessingActivityAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    public DataProcessingActivityAuditLog(
        Guid dataProcessingActivityId,
        Guid userId,
        DataProcessingActivityAuditAction action,
        string newState,
        string? previousState = null,
        string? changeReason = null)
    {
        if (dataProcessingActivityId == Guid.Empty)
        {
            throw new ArgumentException("Data processing activity ID is required.", nameof(dataProcessingActivityId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(newState))
        {
            throw new ArgumentException("New state is required.", nameof(newState));
        }

        Id = Guid.NewGuid();
        DataProcessingActivityId = dataProcessingActivityId;
        UserId = userId;
        Action = action;
        PreviousState = previousState;
        NewState = newState;
        ChangeReason = changeReason?.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Defines the types of actions that can be audited for data processing activities.
/// </summary>
public enum DataProcessingActivityAuditAction
{
    /// <summary>
    /// A new processing activity was created.
    /// </summary>
    Created = 0,

    /// <summary>
    /// An existing processing activity was updated.
    /// </summary>
    Updated = 1,

    /// <summary>
    /// A processing activity was archived (soft-deleted).
    /// </summary>
    Archived = 2,

    /// <summary>
    /// An archived processing activity was reactivated.
    /// </summary>
    Reactivated = 3
}
