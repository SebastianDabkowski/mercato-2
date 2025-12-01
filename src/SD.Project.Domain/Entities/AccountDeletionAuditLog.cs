namespace SD.Project.Domain.Entities;

/// <summary>
/// Type of account deletion audit event.
/// </summary>
public enum AccountDeletionAuditAction
{
    /// <summary>User requested account deletion.</summary>
    Requested,
    /// <summary>System informed user about deletion impact.</summary>
    ImpactDisplayed,
    /// <summary>User confirmed the deletion.</summary>
    Confirmed,
    /// <summary>Account data was anonymized.</summary>
    Anonymized,
    /// <summary>Deletion was blocked due to conditions.</summary>
    Blocked,
    /// <summary>User cancelled the deletion request.</summary>
    Cancelled
}

/// <summary>
/// Audit log entry for account deletion events.
/// Records who triggered the deletion, for which account, and when,
/// without exposing the deleted personal data.
/// </summary>
public class AccountDeletionAuditLog
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Reference to the deletion request.
    /// </summary>
    public Guid DeletionRequestId { get; private set; }

    /// <summary>
    /// The ID of the user whose account was affected.
    /// This ID is retained for audit purposes even after anonymization.
    /// </summary>
    public Guid AffectedUserId { get; private set; }

    /// <summary>
    /// The ID of the user who triggered this action (usually the same as AffectedUserId,
    /// but could be an admin in some scenarios).
    /// </summary>
    public Guid TriggeredByUserId { get; private set; }

    /// <summary>
    /// The role of the user who triggered the action.
    /// </summary>
    public UserRole TriggeredByRole { get; private set; }

    /// <summary>
    /// The type of audit action.
    /// </summary>
    public AccountDeletionAuditAction Action { get; private set; }

    /// <summary>
    /// Additional context or notes about the action.
    /// Should not contain personal data.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string from which the action was performed.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    private AccountDeletionAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new account deletion audit log entry.
    /// </summary>
    public AccountDeletionAuditLog(
        Guid deletionRequestId,
        Guid affectedUserId,
        Guid triggeredByUserId,
        UserRole triggeredByRole,
        AccountDeletionAuditAction action,
        string? notes = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (deletionRequestId == Guid.Empty)
        {
            throw new ArgumentException("Deletion request ID is required.", nameof(deletionRequestId));
        }

        if (affectedUserId == Guid.Empty)
        {
            throw new ArgumentException("Affected user ID is required.", nameof(affectedUserId));
        }

        if (triggeredByUserId == Guid.Empty)
        {
            throw new ArgumentException("Triggered by user ID is required.", nameof(triggeredByUserId));
        }

        Id = Guid.NewGuid();
        DeletionRequestId = deletionRequestId;
        AffectedUserId = affectedUserId;
        TriggeredByUserId = triggeredByUserId;
        TriggeredByRole = triggeredByRole;
        Action = action;
        Notes = notes?.Trim();
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
        OccurredAt = DateTime.UtcNow;
    }
}
