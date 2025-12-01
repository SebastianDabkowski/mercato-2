namespace SD.Project.Application.Interfaces;

/// <summary>
/// Defines the contract for field-level data encryption operations.
/// This service encrypts sensitive data at the application layer before storage.
/// </summary>
public interface IDataEncryptionService
{
    /// <summary>
    /// Encrypts plain text data using the configured encryption key.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted data as a Base64-encoded string, or null if input is null.</returns>
    string? Encrypt(string? plainText);

    /// <summary>
    /// Decrypts encrypted data using the configured encryption key.
    /// </summary>
    /// <param name="encryptedText">The encrypted data as a Base64-encoded string.</param>
    /// <returns>The decrypted plain text, or null if input is null.</returns>
    string? Decrypt(string? encryptedText);

    /// <summary>
    /// Indicates whether encryption is enabled and properly configured.
    /// </summary>
    bool IsEnabled { get; }
}
