using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of an external login attempt.
/// </summary>
public sealed record ExternalLoginResultDto
{
    public bool Success { get; init; }
    public Guid? UserId { get; init; }
    public UserRole? Role { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsNewUser { get; init; }

    private ExternalLoginResultDto() { }

    public static ExternalLoginResultDto Succeeded(Guid userId, UserRole role, string email, string firstName, bool isNewUser)
    {
        return new ExternalLoginResultDto
        {
            Success = true,
            UserId = userId,
            Role = role,
            Email = email,
            FirstName = firstName,
            IsNewUser = isNewUser
        };
    }

    public static ExternalLoginResultDto Failed(string errorMessage)
    {
        return new ExternalLoginResultDto
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
