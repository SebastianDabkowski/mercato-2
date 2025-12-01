using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// Extension methods for user role and status display formatting.
/// </summary>
public static class UserDisplayExtensions
{
    public static string ToDisplayName(this UserRole role) => role switch
    {
        UserRole.Buyer => "Buyer",
        UserRole.Seller => "Seller",
        UserRole.Admin => "Admin",
        _ => role.ToString()
    };

    public static string ToBadgeClass(this UserRole role) => role switch
    {
        UserRole.Admin => "bg-primary",
        UserRole.Seller => "bg-info",
        UserRole.Buyer => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string ToDisplayName(this UserStatus status) => status switch
    {
        UserStatus.Unverified => "Pending Verification",
        UserStatus.Verified => "Active",
        UserStatus.Suspended => "Blocked",
        _ => status.ToString()
    };

    public static string ToBadgeClass(this UserStatus status) => status switch
    {
        UserStatus.Unverified => "bg-warning text-dark",
        UserStatus.Verified => "bg-success",
        UserStatus.Suspended => "bg-danger",
        _ => "bg-secondary"
    };

    public static string ToDisplayName(this KycStatus status) => status switch
    {
        KycStatus.NotStarted => "Not Started",
        KycStatus.Pending => "Pending Review",
        KycStatus.Approved => "Approved",
        KycStatus.Rejected => "Rejected",
        _ => status.ToString()
    };

    public static string ToBadgeClass(this KycStatus status) => status switch
    {
        KycStatus.NotStarted => "bg-secondary",
        KycStatus.Pending => "bg-warning text-dark",
        KycStatus.Approved => "bg-success",
        KycStatus.Rejected => "bg-danger",
        _ => "bg-secondary"
    };
}

/// <summary>
/// View model for a user in the admin user list.
/// </summary>
public sealed class AdminUserViewModel
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public UserStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsEmailVerified { get; init; }

    public string FullName => $"{FirstName} {LastName}";
    public string RoleDisplayName => Role.ToDisplayName();
    public string StatusDisplayName => Status.ToDisplayName();
    public string StatusBadgeClass => Status.ToBadgeClass();
    public string RoleBadgeClass => Role.ToBadgeClass();
}

/// <summary>
/// View model for detailed user information in admin user detail view.
/// </summary>
public sealed class AdminUserDetailViewModel
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public UserStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsEmailVerified { get; init; }
    public DateTime? EmailVerifiedAt { get; init; }
    public string? CompanyName { get; init; }
    public string? PhoneNumber { get; init; }
    public KycStatus KycStatus { get; init; }
    public DateTime? KycSubmittedAt { get; init; }
    public DateTime? KycReviewedAt { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public DateTime? TwoFactorEnabledAt { get; init; }
    public IReadOnlyList<LoginEventViewModel> RecentLoginActivity { get; init; } = Array.Empty<LoginEventViewModel>();

    public string FullName => $"{FirstName} {LastName}";
    public string RoleDisplayName => Role.ToDisplayName();
    public string StatusDisplayName => Status.ToDisplayName();
    public string StatusBadgeClass => Status.ToBadgeClass();
    public string RoleBadgeClass => Role.ToBadgeClass();
    public string KycStatusDisplayName => KycStatus.ToDisplayName();
    public string KycStatusBadgeClass => KycStatus.ToBadgeClass();
}

/// <summary>
/// View model for a login event in user detail view.
/// </summary>
public sealed class LoginEventViewModel
{
    public DateTime OccurredAt { get; init; }
    public bool IsSuccess { get; init; }
    public LoginEventType EventType { get; init; }
    public string? IpAddress { get; init; }
    public string? Location { get; init; }
    public string? FailureReason { get; init; }

    public string EventTypeDisplayName => EventType switch
    {
        LoginEventType.Password => "Password",
        LoginEventType.Social => "Social Login",
        LoginEventType.TwoFactor => "2FA",
        LoginEventType.RecoveryCode => "Recovery Code",
        LoginEventType.SessionRefresh => "Session Refresh",
        LoginEventType.Logout => "Logout",
        _ => EventType.ToString()
    };

    public string StatusIcon => IsSuccess ? "bi-check-circle-fill text-success" : "bi-x-circle-fill text-danger";
    public string StatusText => IsSuccess ? "Success" : "Failed";
}
