namespace SD.Project.Application.Interfaces;

/// <summary>
/// Abstraction for password hashing and verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
