using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of a login attempt.
/// </summary>
public sealed record LoginResultDto
{
    public bool Success { get; init; }
    public Guid? UserId { get; init; }
    public UserRole? Role { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? ErrorMessage { get; init; }
    public bool RequiresEmailVerification { get; init; }
    public bool RequiresTwoFactor { get; init; }

    public static LoginResultDto Succeeded(Guid userId, UserRole role, string email, string firstName) =>
        new()
        {
            Success = true,
            UserId = userId,
            Role = role,
            Email = email,
            FirstName = firstName
        };

    public static LoginResultDto Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };

    public static LoginResultDto VerificationRequired(string email) =>
        new()
        {
            Success = false,
            ErrorMessage = "Your email address has not been verified. Please check your inbox for the verification email.",
            RequiresEmailVerification = true,
            Email = email
        };

    public static LoginResultDto TwoFactorRequired(Guid userId, string email) =>
        new()
        {
            Success = false,
            UserId = userId,
            Email = email,
            RequiresTwoFactor = true
        };
}
