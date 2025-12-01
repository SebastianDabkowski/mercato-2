using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Summary user information for admin user list.
/// </summary>
public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    UserStatus Status,
    DateTime CreatedAt,
    bool IsEmailVerified);

/// <summary>
/// Detailed user information for admin user detail view.
/// </summary>
public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    UserStatus Status,
    DateTime CreatedAt,
    bool IsEmailVerified,
    DateTime? EmailVerifiedAt,
    string? CompanyName,
    string? PhoneNumber,
    KycStatus KycStatus,
    DateTime? KycSubmittedAt,
    DateTime? KycReviewedAt,
    bool TwoFactorEnabled,
    DateTime? TwoFactorEnabledAt,
    IReadOnlyList<LoginEventSummaryDto> RecentLoginActivity,
    UserBlockInfoDto? BlockInfo);

/// <summary>
/// Summary information about a login event.
/// </summary>
public sealed record LoginEventSummaryDto(
    DateTime OccurredAt,
    bool IsSuccess,
    LoginEventType EventType,
    string? IpAddress,
    string? Location,
    string? FailureReason);

/// <summary>
/// Information about an active block on a user account.
/// </summary>
public sealed record UserBlockInfoDto(
    Guid BlockedByAdminId,
    string BlockedByAdminEmail,
    DateTime BlockedAt,
    BlockReason Reason,
    string? Notes);

/// <summary>
/// Historical record of a block/reactivate event for a user account.
/// </summary>
public sealed record UserBlockHistoryItemDto(
    Guid Id,
    Guid BlockedByAdminId,
    string BlockedByAdminName,
    DateTime BlockedAt,
    BlockReason Reason,
    string? BlockNotes,
    bool IsActive,
    Guid? ReactivatedByAdminId,
    string? ReactivatedByAdminName,
    DateTime? ReactivatedAt,
    string? ReactivationNotes);

/// <summary>
/// Result of a block or reactivate operation.
/// </summary>
public sealed record BlockUserResultDto(
    bool IsSuccess,
    string? ErrorMessage)
{
    public static BlockUserResultDto Success() => new(true, null);
    public static BlockUserResultDto Failed(string message) => new(false, message);
}
