namespace SD.Project.Application.Interfaces;

/// <summary>
/// Abstraction for password validation according to security policy.
/// </summary>
public interface IPasswordValidator
{
    /// <summary>
    /// Validates a password against security policy requirements.
    /// </summary>
    /// <returns>Collection of validation error messages, empty if valid.</returns>
    IReadOnlyCollection<string> Validate(string password);
}
