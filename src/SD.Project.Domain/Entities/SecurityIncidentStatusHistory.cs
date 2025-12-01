namespace SD.Project.Domain.Entities;

/// <summary>
/// Tracks the history of status changes for a security incident.
/// Provides an audit trail of who changed the status and when.
/// </summary>
public class SecurityIncidentStatusHistory
{
    /// <summary>
    /// Maximum length for notes field.
    /// </summary>
    private const int MaxNotesLength = 2000;

    /// <summary>
    /// Unique identifier for this status history entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the security incident this entry belongs to.
    /// </summary>
    public Guid SecurityIncidentId { get; private set; }

    /// <summary>
    /// The status that was set.
    /// </summary>
    public SecurityIncidentStatus Status { get; private set; }

    /// <summary>
    /// The previous status before this change.
    /// </summary>
    public SecurityIncidentStatus? PreviousStatus { get; private set; }

    /// <summary>
    /// The ID of the user who made the status change.
    /// Null if the change was made by the system (e.g., initial detection).
    /// </summary>
    public Guid? ChangedByUserId { get; private set; }

    /// <summary>
    /// Optional notes about the status change.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// UTC timestamp when the status change occurred.
    /// </summary>
    public DateTime ChangedAt { get; private set; }

    private SecurityIncidentStatusHistory()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new status history entry.
    /// </summary>
    /// <param name="securityIncidentId">The ID of the security incident.</param>
    /// <param name="status">The new status.</param>
    /// <param name="changedByUserId">The ID of the user making the change.</param>
    /// <param name="notes">Optional notes about the change.</param>
    /// <param name="previousStatus">The previous status before this change.</param>
    public SecurityIncidentStatusHistory(
        Guid securityIncidentId,
        SecurityIncidentStatus status,
        Guid? changedByUserId,
        string? notes = null,
        SecurityIncidentStatus? previousStatus = null)
    {
        if (securityIncidentId == Guid.Empty)
        {
            throw new ArgumentException("Security incident ID is required.", nameof(securityIncidentId));
        }

        Id = Guid.NewGuid();
        SecurityIncidentId = securityIncidentId;
        Status = status;
        PreviousStatus = previousStatus;
        ChangedByUserId = changedByUserId;
        Notes = notes?.Length > MaxNotesLength ? notes[..MaxNotesLength] : notes;
        ChangedAt = DateTime.UtcNow;
    }
}
