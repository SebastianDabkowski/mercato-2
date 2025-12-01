namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the type of sensitive resource being accessed.
/// </summary>
public enum SensitiveResourceType
{
    /// <summary>Full customer profile with personal data.</summary>
    CustomerProfile,
    /// <summary>Payout/payment settings and bank details.</summary>
    PayoutDetails,
    /// <summary>Order with full buyer information.</summary>
    OrderDetails,
    /// <summary>Store with seller personal information.</summary>
    StoreDetails,
    /// <summary>Settlement/financial records.</summary>
    SettlementDetails,
    /// <summary>KYC verification documents.</summary>
    KycDocuments
}

/// <summary>
/// Represents the type of action performed on a sensitive resource.
/// </summary>
public enum SensitiveAccessAction
{
    /// <summary>Viewed the resource.</summary>
    View,
    /// <summary>Exported or downloaded the resource.</summary>
    Export,
    /// <summary>Modified the resource.</summary>
    Modify
}

/// <summary>
/// Audit log entry for tracking when admin or support users access sensitive data.
/// This entity supports compliance requirements for monitoring access to personal/financial data.
/// </summary>
public class SensitiveAccessAuditLog
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user who accessed the sensitive resource.
    /// </summary>
    public Guid AccessedByUserId { get; private set; }

    /// <summary>
    /// The role of the user who accessed the resource.
    /// </summary>
    public UserRole AccessedByRole { get; private set; }

    /// <summary>
    /// The type of sensitive resource that was accessed.
    /// </summary>
    public SensitiveResourceType ResourceType { get; private set; }

    /// <summary>
    /// The unique identifier of the accessed resource.
    /// </summary>
    public Guid ResourceId { get; private set; }

    /// <summary>
    /// The ID of the user whose data was accessed (if applicable).
    /// This is the owner of the sensitive data, not the accessor.
    /// </summary>
    public Guid? ResourceOwnerId { get; private set; }

    /// <summary>
    /// The type of action performed on the resource.
    /// </summary>
    public SensitiveAccessAction Action { get; private set; }

    /// <summary>
    /// Additional context or reason for the access (optional).
    /// </summary>
    public string? AccessReason { get; private set; }

    /// <summary>
    /// The IP address from which the access was made.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// The user agent string of the browser/client used for access.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// The UTC timestamp when the access occurred.
    /// </summary>
    public DateTime AccessedAt { get; private set; }

    private SensitiveAccessAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new audit log entry for sensitive data access.
    /// </summary>
    /// <param name="accessedByUserId">The ID of the user accessing the data.</param>
    /// <param name="accessedByRole">The role of the user accessing the data.</param>
    /// <param name="resourceType">The type of sensitive resource being accessed.</param>
    /// <param name="resourceId">The unique identifier of the resource.</param>
    /// <param name="action">The action performed on the resource.</param>
    /// <param name="resourceOwnerId">The ID of the data owner (optional).</param>
    /// <param name="accessReason">Additional context for the access (optional).</param>
    /// <param name="ipAddress">The IP address of the accessor (optional).</param>
    /// <param name="userAgent">The user agent string (optional).</param>
    public SensitiveAccessAuditLog(
        Guid accessedByUserId,
        UserRole accessedByRole,
        SensitiveResourceType resourceType,
        Guid resourceId,
        SensitiveAccessAction action,
        Guid? resourceOwnerId = null,
        string? accessReason = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (accessedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Accessor user ID is required.", nameof(accessedByUserId));
        }

        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException("Resource ID is required.", nameof(resourceId));
        }

        Id = Guid.NewGuid();
        AccessedByUserId = accessedByUserId;
        AccessedByRole = accessedByRole;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Action = action;
        ResourceOwnerId = resourceOwnerId;
        AccessReason = accessReason?.Trim();
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
        AccessedAt = DateTime.UtcNow;
    }
}
