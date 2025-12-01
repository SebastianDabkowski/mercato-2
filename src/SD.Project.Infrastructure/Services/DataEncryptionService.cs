using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Configuration options for data encryption.
/// </summary>
public class DataEncryptionOptions
{
    /// <summary>
    /// Indicates whether field-level encryption is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The purpose string used to derive encryption keys from the Data Protection system.
    /// This should be unique per data type/context.
    /// </summary>
    public string Purpose { get; set; } = "SD.Project.SensitiveData.v1";

    /// <summary>
    /// The path where encryption keys are stored.
    /// Should be configured to use a secure key management service in production.
    /// </summary>
    public string? KeysPath { get; set; }
}

/// <summary>
/// Implements field-level encryption using ASP.NET Core Data Protection API.
/// Data Protection API provides secure key management, key rotation, and encryption.
/// </summary>
/// <remarks>
/// In production, configure Data Protection to use:
/// - Azure Key Vault or Azure Blob Storage for key persistence
/// - AWS KMS for key encryption
/// - Or another managed key storage solution
/// 
/// Keys should NOT be stored in source code or unprotected configuration files.
/// </remarks>
public class DataEncryptionService : IDataEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<DataEncryptionService> _logger;
    private readonly bool _isEnabled;

    public DataEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<DataEncryptionOptions> options,
        ILogger<DataEncryptionService> logger)
    {
        var encryptionOptions = options.Value;
        _protector = dataProtectionProvider.CreateProtector(encryptionOptions.Purpose);
        _logger = logger;
        _isEnabled = encryptionOptions.Enabled;
    }

    /// <inheritdoc/>
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc/>
    public string? Encrypt(string? plainText)
    {
        if (plainText is null)
        {
            return null;
        }

        if (!_isEnabled)
        {
            return plainText;
        }

        try
        {
            return _protector.Protect(plainText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw new InvalidOperationException("Data encryption failed", ex);
        }
    }

    /// <inheritdoc/>
    public string? Decrypt(string? encryptedText)
    {
        if (encryptedText is null)
        {
            return null;
        }

        if (!_isEnabled)
        {
            return encryptedText;
        }

        try
        {
            return _protector.Unprotect(encryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data. This may indicate key rotation issues or corrupted data.");
            throw new InvalidOperationException("Data decryption failed", ex);
        }
    }
}
