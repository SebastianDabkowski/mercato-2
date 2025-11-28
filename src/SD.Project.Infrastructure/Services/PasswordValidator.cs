using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Password validation according to security policy.
/// Requirements: minimum 8 characters, uppercase, lowercase, digit, no common passwords.
/// </summary>
public sealed class PasswordValidator : IPasswordValidator
{
    private const int MinLength = 8;
    private const int MaxLength = 128;

    // Common passwords that are not allowed
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password1", "password123", "123456", "12345678", "123456789",
        "qwerty", "qwerty123", "abc123", "letmein", "welcome", "admin", "login",
        "passw0rd", "master", "hello", "monkey", "dragon", "111111", "baseball",
        "iloveyou", "trustno1", "sunshine", "princess", "football", "shadow"
    };

    public IReadOnlyCollection<string> Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return errors;
        }

        if (password.Length < MinLength)
        {
            errors.Add($"Password must be at least {MinLength} characters long.");
        }

        if (password.Length > MaxLength)
        {
            errors.Add($"Password must be at most {MaxLength} characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (CommonPasswords.Contains(password))
        {
            errors.Add("Password is too common. Please choose a more secure password.");
        }

        return errors;
    }
}
