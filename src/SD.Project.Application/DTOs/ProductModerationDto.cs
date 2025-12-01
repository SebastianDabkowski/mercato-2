using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for displaying a product in the moderation queue.
/// </summary>
public record ProductModerationDto(
    Guid ProductId,
    Guid? StoreId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int Stock,
    string Category,
    ProductStatus Status,
    ProductModerationStatus ModerationStatus,
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
/// DTO for product moderation statistics.
/// </summary>
public record ProductModerationStatsDto(
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int ApprovedTodayCount,
    int RejectedTodayCount);

/// <summary>
/// Result of a product moderation action.
/// </summary>
public record ProductModerationResultDto(
    bool Success,
    string? ErrorMessage = null,
    Guid? ProductId = null)
{
    public static ProductModerationResultDto Succeeded(Guid productId) => new(true, null, productId);
    public static ProductModerationResultDto Failed(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Result of a batch product moderation action.
/// </summary>
public record BatchProductModerationResultDto(
    bool Success,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string>? Errors = null)
{
    public static BatchProductModerationResultDto Succeeded(int successCount) =>
        new(true, successCount, 0);

    public static BatchProductModerationResultDto PartialSuccess(int successCount, int failureCount, IReadOnlyList<string> errors) =>
        new(successCount > 0, successCount, failureCount, errors);

    public static BatchProductModerationResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, 0, errors.Count, errors);
}

/// <summary>
/// DTO for product moderation audit log entry.
/// </summary>
public record ProductModerationAuditLogDto(
    Guid Id,
    Guid ProductId,
    Guid ModeratorId,
    string? ModeratorName,
    ProductModerationStatus Decision,
    string? Reason,
    DateTime CreatedAt);
