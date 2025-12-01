namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the types of critical actions that are audit logged.
/// </summary>
public enum CriticalActionType
{
    /// <summary>User login attempt.</summary>
    Login,
    /// <summary>User logout.</summary>
    Logout,
    /// <summary>User role change.</summary>
    RoleChange,
    /// <summary>Payout settings change.</summary>
    PayoutChange,
    /// <summary>Order status override by admin/support.</summary>
    OrderStatusOverride,
    /// <summary>Refund initiated.</summary>
    Refund,
    /// <summary>Account deletion request or execution.</summary>
    AccountDeletion,
    /// <summary>Password change.</summary>
    PasswordChange,
    /// <summary>Two-factor authentication change.</summary>
    TwoFactorChange,
    /// <summary>Permission or role assignment change.</summary>
    PermissionChange,
    /// <summary>Settlement adjustment.</summary>
    SettlementAdjustment,
    /// <summary>Sensitive data export.</summary>
    DataExport,
    /// <summary>User block or unblock.</summary>
    UserBlock,
    /// <summary>Store status change.</summary>
    StoreStatusChange
}

/// <summary>
/// Defines the outcome of a critical action.
/// </summary>
public enum CriticalActionOutcome
{
    /// <summary>The action completed successfully.</summary>
    Success,
    /// <summary>The action failed.</summary>
    Failure
}

/// <summary>
/// Audit log entry for critical actions in the system.
/// Provides a tamper-evident record of security-sensitive operations
/// for compliance, investigation, and monitoring purposes.
/// </summary>
public class CriticalActionAuditLog
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user who performed the action.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The role of the user at the time of the action.
    /// </summary>
    public UserRole UserRole { get; private set; }

    /// <summary>
    /// The type of critical action performed.
    /// </summary>
    public CriticalActionType ActionType { get; private set; }

    /// <summary>
    /// The type of resource the action was performed on.
    /// </summary>
    public string TargetResourceType { get; private set; } = default!;

    /// <summary>
    /// The unique identifier of the target resource (optional).
    /// </summary>
    public Guid? TargetResourceId { get; private set; }

    /// <summary>
    /// The outcome of the action.
    /// </summary>
    public CriticalActionOutcome Outcome { get; private set; }

    /// <summary>
    /// Additional details about the action (e.g., old/new values, error message).
    /// Should not contain sensitive data like passwords.
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// The IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// The user agent string of the client.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Correlation ID for tracing related actions.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// The UTC timestamp when the action occurred.
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this record should be deleted for retention compliance.
    /// </summary>
    public DateTime RetentionExpiresAt { get; private set; }

    private CriticalActionAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new critical action audit log entry.
    /// </summary>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="userRole">The role of the user at the time of the action.</param>
    /// <param name="actionType">The type of critical action.</param>
    /// <param name="targetResourceType">The type of resource affected.</param>
    /// <param name="targetResourceId">The ID of the target resource (optional).</param>
    /// <param name="outcome">The outcome of the action.</param>
    /// <param name="details">Additional details about the action (optional).</param>
    /// <param name="ipAddress">The IP address of the client (optional).</param>
    /// <param name="userAgent">The user agent string (optional).</param>
    /// <param name="correlationId">Correlation ID for tracing (optional).</param>
    /// <param name="retentionDays">Number of days to retain this record (default 365 days for critical actions).</param>
    public CriticalActionAuditLog(
        Guid userId,
        UserRole userRole,
        CriticalActionType actionType,
        string targetResourceType,
        Guid? targetResourceId,
        CriticalActionOutcome outcome,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        int retentionDays = 365)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(targetResourceType))
        {
            throw new ArgumentException("Target resource type is required.", nameof(targetResourceType));
        }

        if (retentionDays < 1)
        {
            throw new ArgumentException("Retention days must be at least 1.", nameof(retentionDays));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        UserRole = userRole;
        ActionType = actionType;
        TargetResourceType = targetResourceType.Trim();
        TargetResourceId = targetResourceId;
        Outcome = outcome;
        Details = details?.Length > 4000 ? details[..4000] : details;
        IpAddress = ipAddress?.Length > 45 ? ipAddress[..45] : ipAddress; // IPv6 max length
        UserAgent = userAgent?.Length > 512 ? userAgent[..512] : userAgent;
        CorrelationId = correlationId?.Length > 100 ? correlationId[..100] : correlationId;
        OccurredAt = DateTime.UtcNow;
        RetentionExpiresAt = OccurredAt.AddDays(retentionDays);
    }
}
