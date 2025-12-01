namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the type of moderation action taken on a seller rating.
/// </summary>
public enum SellerRatingModerationAction
{
    /// <summary>Rating was approved by moderator.</summary>
    Approved,
    /// <summary>Rating was rejected by moderator.</summary>
    Rejected,
    /// <summary>Rating visibility was changed.</summary>
    VisibilityChanged,
    /// <summary>Rating was flagged for moderation.</summary>
    Flagged,
    /// <summary>Rating flag was cleared.</summary>
    FlagCleared,
    /// <summary>Rating was manually reported by a user.</summary>
    Reported
}

/// <summary>
/// Audit log entry for seller rating moderation actions.
/// Records all moderation decisions for compliance and traceability.
/// </summary>
public class SellerRatingModerationAuditLog
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The seller rating that was moderated.
    /// </summary>
    public Guid SellerRatingId { get; private set; }

    /// <summary>
    /// The admin or moderator who performed the action.
    /// Null if the action was automated.
    /// </summary>
    public Guid? ModeratorId { get; private set; }

    /// <summary>
    /// The type of moderation action taken.
    /// </summary>
    public SellerRatingModerationAction Action { get; private set; }

    /// <summary>
    /// The previous moderation status before this action.
    /// </summary>
    public SellerRatingModerationStatus PreviousStatus { get; private set; }

    /// <summary>
    /// The new moderation status after this action.
    /// </summary>
    public SellerRatingModerationStatus NewStatus { get; private set; }

    /// <summary>
    /// Reason provided for the moderation action.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Additional notes from the moderator.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Whether this was an automated action (e.g., by keyword filter).
    /// </summary>
    public bool IsAutomated { get; private set; }

    /// <summary>
    /// The automated rule that triggered the action, if applicable.
    /// </summary>
    public string? AutomatedRuleName { get; private set; }

    /// <summary>
    /// When the moderation action occurred.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// IP address of the moderator, for security auditing.
    /// </summary>
    public string? IpAddress { get; private set; }

    private SellerRatingModerationAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new audit log entry for a manual moderation action.
    /// </summary>
    public SellerRatingModerationAuditLog(
        Guid sellerRatingId,
        Guid moderatorId,
        SellerRatingModerationAction action,
        SellerRatingModerationStatus previousStatus,
        SellerRatingModerationStatus newStatus,
        string? reason = null,
        string? notes = null,
        string? ipAddress = null)
    {
        if (sellerRatingId == Guid.Empty)
        {
            throw new ArgumentException("Seller rating ID is required.", nameof(sellerRatingId));
        }

        if (moderatorId == Guid.Empty)
        {
            throw new ArgumentException("Moderator ID is required for manual moderation.", nameof(moderatorId));
        }

        Id = Guid.NewGuid();
        SellerRatingId = sellerRatingId;
        ModeratorId = moderatorId;
        Action = action;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = reason?.Trim();
        Notes = notes?.Trim();
        IsAutomated = false;
        IpAddress = ipAddress?.Length > 45 ? ipAddress[..45] : ipAddress;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new audit log entry for an automated moderation action.
    /// </summary>
    public static SellerRatingModerationAuditLog CreateAutomated(
        Guid sellerRatingId,
        SellerRatingModerationAction action,
        SellerRatingModerationStatus previousStatus,
        SellerRatingModerationStatus newStatus,
        string ruleName,
        string? reason = null)
    {
        if (sellerRatingId == Guid.Empty)
        {
            throw new ArgumentException("Seller rating ID is required.", nameof(sellerRatingId));
        }

        if (string.IsNullOrWhiteSpace(ruleName))
        {
            throw new ArgumentException("Rule name is required for automated moderation.", nameof(ruleName));
        }

        return new SellerRatingModerationAuditLog
        {
            Id = Guid.NewGuid(),
            SellerRatingId = sellerRatingId,
            ModeratorId = null,
            Action = action,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Reason = reason?.Trim(),
            Notes = null,
            IsAutomated = true,
            AutomatedRuleName = ruleName.Trim(),
            IpAddress = null,
            CreatedAt = DateTime.UtcNow
        };
    }
}
