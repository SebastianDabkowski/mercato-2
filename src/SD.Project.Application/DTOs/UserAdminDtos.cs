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
    IReadOnlyList<LoginEventSummaryDto> RecentLoginActivity);

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
