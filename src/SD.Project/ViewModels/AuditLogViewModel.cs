using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying audit log entries in the admin UI.
/// </summary>
public sealed class AuditLogViewModel
{
    /// <summary>
    /// Unique identifier for the audit log entry.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The ID of the user who performed the action.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The email of the user who performed the action (for display).
    /// </summary>
    public string? UserEmail { get; init; }

    /// <summary>
    /// The role of the user at the time of the action.
    /// </summary>
    public string UserRole { get; init; } = string.Empty;

    /// <summary>
    /// The type of action performed.
    /// </summary>
    public string ActionType { get; init; } = string.Empty;

    /// <summary>
    /// The type of resource the action was performed on.
    /// </summary>
    public string TargetResourceType { get; init; } = string.Empty;

    /// <summary>
    /// The ID of the target resource.
    /// </summary>
    public Guid? TargetResourceId { get; init; }

    /// <summary>
    /// The outcome of the action (Success/Failure).
    /// </summary>
    public string Outcome { get; init; } = string.Empty;

    /// <summary>
    /// Additional details about the action.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// The IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// The UTC timestamp when the action occurred.
    /// </summary>
    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// Returns a display-friendly short ID for the user.
    /// </summary>
    public string ShortUserId
    {
        get
        {
            var idStr = UserId.ToString();
            return idStr.Length >= 8 ? idStr[..8] + "..." : idStr;
        }
    }

    /// <summary>
    /// Returns a display-friendly short ID for the target resource.
    /// </summary>
    public string ShortTargetResourceId
    {
        get
        {
            if (!TargetResourceId.HasValue)
            {
                return "-";
            }
            var idStr = TargetResourceId.Value.ToString();
            return idStr.Length >= 8 ? idStr[..8] + "..." : idStr;
        }
    }

    /// <summary>
    /// Returns the Bootstrap badge class for the outcome.
    /// </summary>
    public string OutcomeBadgeClass => Outcome switch
    {
        "Success" => "bg-success",
        "Failure" => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Returns a display-friendly action type name.
    /// </summary>
    public string ActionTypeDisplayName => ActionType switch
    {
        "Login" => "Login",
        "Logout" => "Logout",
        "RoleChange" => "Role Change",
        "PayoutChange" => "Payout Change",
        "OrderStatusOverride" => "Order Status Override",
        "Refund" => "Refund",
        "AccountDeletion" => "Account Deletion",
        "PasswordChange" => "Password Change",
        "TwoFactorChange" => "2FA Change",
        "PermissionChange" => "Permission Change",
        "SettlementAdjustment" => "Settlement Adjustment",
        "DataExport" => "Data Export",
        "UserBlock" => "User Block/Unblock",
        "StoreStatusChange" => "Store Status Change",
        _ => ActionType
    };

    /// <summary>
    /// Returns the Bootstrap icon class for the action type.
    /// </summary>
    public string ActionTypeIcon => ActionType switch
    {
        "Login" => "bi-box-arrow-in-right",
        "Logout" => "bi-box-arrow-right",
        "RoleChange" => "bi-person-badge",
        "PayoutChange" => "bi-credit-card",
        "OrderStatusOverride" => "bi-bag-check",
        "Refund" => "bi-arrow-counterclockwise",
        "AccountDeletion" => "bi-person-x",
        "PasswordChange" => "bi-key",
        "TwoFactorChange" => "bi-shield-lock",
        "PermissionChange" => "bi-shield-check",
        "SettlementAdjustment" => "bi-calculator",
        "DataExport" => "bi-download",
        "UserBlock" => "bi-slash-circle",
        "StoreStatusChange" => "bi-shop",
        _ => "bi-activity"
    };
}
