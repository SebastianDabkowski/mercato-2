using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a product in the moderation queue.
/// </summary>
public record ProductModerationViewModel(
    Guid ProductId,
    Guid? StoreId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int Stock,
    string Category,
    string Status,
    string ModerationStatus,
    string? ModerationRejectionReason,
    Guid? LastModeratorId,
    string? LastModeratorName,
    DateTime? LastModeratedAt,
    string? StoreName,
    string? SellerName,
    string? SellerEmail,
    string? MainImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// View model for product moderation statistics.
/// </summary>
public record ProductModerationStatsViewModel(
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int ApprovedTodayCount,
    int RejectedTodayCount);

/// <summary>
/// View model for product moderation audit log entry.
/// </summary>
public record ProductModerationAuditLogViewModel(
    Guid Id,
    Guid ProductId,
    Guid ModeratorId,
    string? ModeratorName,
    string Decision,
    string? Reason,
    DateTime CreatedAt);

/// <summary>
/// Helper class for product moderation status display.
/// </summary>
public static class ProductModerationStatusHelper
{
    public static string GetStatusDisplayName(string status)
    {
        return status switch
        {
            "PendingReview" => "Pending Review",
            "Approved" => "Approved",
            "Rejected" => "Rejected",
            _ => status
        };
    }

    public static string GetStatusBadgeClass(string status)
    {
        return status switch
        {
            "PendingReview" => "bg-warning text-dark",
            "Approved" => "bg-success",
            "Rejected" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public static string GetProductStatusDisplayName(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.Draft => "Draft",
            ProductStatus.Active => "Active",
            ProductStatus.Inactive => "Inactive",
            ProductStatus.Archived => "Archived",
            ProductStatus.Suspended => "Suspended",
            _ => status.ToString()
        };
    }

    public static string GetProductStatusBadgeClass(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.Draft => "bg-secondary",
            ProductStatus.Active => "bg-success",
            ProductStatus.Inactive => "bg-warning text-dark",
            ProductStatus.Archived => "bg-dark",
            ProductStatus.Suspended => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
