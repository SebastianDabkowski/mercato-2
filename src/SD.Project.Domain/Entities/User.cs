using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a user account in the marketplace.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; } = default!;
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }

    // External login provider info
    public ExternalLoginProvider ExternalProvider { get; private set; }
    public string? ExternalId { get; private set; }

    // Personal data for KYC and invoicing
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? CompanyName { get; private set; }
    public string? TaxId { get; private set; }
    public string? PhoneNumber { get; private set; }

    public bool AcceptedTerms { get; private set; }
    public DateTime AcceptedTermsAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // KYC status for sellers
    public KycStatus KycStatus { get; private set; }
    public DateTime? KycSubmittedAt { get; private set; }
    public DateTime? KycReviewedAt { get; private set; }

    // Email verification timestamp
    public DateTime? EmailVerifiedAt { get; private set; }

    // Two-factor authentication configuration
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecretKey { get; private set; }
    public string? TwoFactorRecoveryCodes { get; private set; }
    public DateTime? TwoFactorEnabledAt { get; private set; }

    private User()
    {
        // EF Core constructor
    }

    public User(
        Guid id,
        Email email,
        string passwordHash,
        UserRole role,
        string firstName,
        string lastName,
        bool acceptedTerms,
        string? companyName = null,
        string? taxId = null,
        string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        if (!acceptedTerms)
        {
            throw new ArgumentException("Terms must be accepted to create an account.", nameof(acceptedTerms));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash;
        Role = role;
        Status = UserStatus.Unverified;
        ExternalProvider = ExternalLoginProvider.None;
        ExternalId = null;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        CompanyName = companyName?.Trim();
        TaxId = taxId?.Trim();
        PhoneNumber = phoneNumber?.Trim();
        AcceptedTerms = acceptedTerms;
        AcceptedTermsAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        KycStatus = KycStatus.NotStarted;
        KycSubmittedAt = null;
        KycReviewedAt = null;
        EmailVerifiedAt = null;
        TwoFactorEnabled = false;
        TwoFactorSecretKey = null;
        TwoFactorRecoveryCodes = null;
        TwoFactorEnabledAt = null;
    }

    /// <summary>
    /// Creates a new user from an external login provider.
    /// Social login users do not require a password and are automatically verified.
    /// </summary>
    public static User CreateFromExternalLogin(
        Email email,
        ExternalLoginProvider provider,
        string externalId,
        string firstName,
        string lastName)
    {
        if (provider == ExternalLoginProvider.None)
        {
            throw new ArgumentException("External provider must be specified for external login.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("External ID is required.", nameof(externalId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? throw new ArgumentNullException(nameof(email)),
            PasswordHash = null, // No password for external logins
            Role = UserRole.Buyer, // Social login is only for buyers
            Status = UserStatus.Verified, // Social login users are automatically verified
            ExternalProvider = provider,
            ExternalId = externalId.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            CompanyName = null,
            TaxId = null,
            PhoneNumber = null,
            AcceptedTerms = true, // By using social login, user implicitly accepts terms
            AcceptedTermsAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            KycStatus = KycStatus.NotStarted,
            KycSubmittedAt = null,
            KycReviewedAt = null,
            EmailVerifiedAt = DateTime.UtcNow, // Social login users are automatically email verified
            TwoFactorEnabled = false,
            TwoFactorSecretKey = null,
            TwoFactorRecoveryCodes = null,
            TwoFactorEnabledAt = null
        };

        return user;
    }

    /// <summary>
    /// Marks the user's email as verified.
    /// </summary>
    public void VerifyEmail()
    {
        if (Status == UserStatus.Suspended)
        {
            throw new InvalidOperationException("Cannot verify email for a suspended account.");
        }

        Status = UserStatus.Verified;
        EmailVerifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initiates the KYC process for the user.
    /// </summary>
    public void StartKyc()
    {
        if (Role != UserRole.Seller)
        {
            throw new InvalidOperationException("KYC is only required for sellers.");
        }

        if (KycStatus != KycStatus.NotStarted && KycStatus != KycStatus.Rejected)
        {
            throw new InvalidOperationException("KYC has already been started or completed.");
        }

        KycStatus = KycStatus.Pending;
        KycSubmittedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approves the user's KYC verification.
    /// </summary>
    public void ApproveKyc()
    {
        if (KycStatus != KycStatus.Pending)
        {
            throw new InvalidOperationException("KYC must be pending to approve.");
        }

        KycStatus = KycStatus.Approved;
        KycReviewedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the user's KYC verification.
    /// </summary>
    public void RejectKyc()
    {
        if (KycStatus != KycStatus.Pending)
        {
            throw new InvalidOperationException("KYC must be pending to reject.");
        }

        KycStatus = KycStatus.Rejected;
        KycReviewedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Indicates whether this seller user requires KYC verification.
    /// </summary>
    public bool RequiresKyc => Role == UserRole.Seller && KycStatus != KycStatus.Approved;

    /// <summary>
    /// Indicates whether the user's email has been verified.
    /// </summary>
    public bool IsEmailVerified => EmailVerifiedAt is not null;

    /// <summary>
    /// Suspends the user account.
    /// </summary>
    public void Suspend()
    {
        Status = UserStatus.Suspended;
    }

    /// <summary>
    /// Blocks the user account. Blocked users cannot log in and seller stores/listings become hidden.
    /// </summary>
    public void Block()
    {
        Status = UserStatus.Blocked;
    }

    /// <summary>
    /// Unblocks the user account, restoring it to verified status.
    /// </summary>
    public void Unblock()
    {
        if (Status != UserStatus.Blocked)
        {
            throw new InvalidOperationException("User is not currently blocked.");
        }

        Status = UserStatus.Verified;
    }

    /// <summary>
    /// Indicates whether the user account is blocked.
    /// </summary>
    public bool IsBlocked => Status == UserStatus.Blocked;

    /// <summary>
    /// Updates the user's password hash. Only valid for users with password-based authentication.
    /// </summary>
    /// <param name="newPasswordHash">The new password hash.</param>
    /// <exception cref="InvalidOperationException">Thrown if the user uses external login.</exception>
    public void SetPassword(string newPasswordHash)
    {
        if (ExternalProvider != ExternalLoginProvider.None)
        {
            throw new InvalidOperationException("Cannot set password for users with external login.");
        }

        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
    }

    /// <summary>
    /// Enables two-factor authentication for the user.
    /// </summary>
    /// <param name="secretKey">The TOTP secret key (base32 encoded).</param>
    /// <param name="recoveryCodes">Comma-separated recovery codes for backup access.</param>
    /// <exception cref="InvalidOperationException">Thrown if 2FA is already enabled.</exception>
    public void EnableTwoFactor(string secretKey, string recoveryCodes)
    {
        if (TwoFactorEnabled)
        {
            throw new InvalidOperationException("Two-factor authentication is already enabled.");
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new ArgumentException("Secret key is required.", nameof(secretKey));
        }

        if (string.IsNullOrWhiteSpace(recoveryCodes))
        {
            throw new ArgumentException("Recovery codes are required.", nameof(recoveryCodes));
        }

        TwoFactorEnabled = true;
        TwoFactorSecretKey = secretKey;
        TwoFactorRecoveryCodes = recoveryCodes;
        TwoFactorEnabledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables two-factor authentication for the user.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if 2FA is not enabled.</exception>
    public void DisableTwoFactor()
    {
        if (!TwoFactorEnabled)
        {
            throw new InvalidOperationException("Two-factor authentication is not enabled.");
        }

        TwoFactorEnabled = false;
        TwoFactorSecretKey = null;
        TwoFactorRecoveryCodes = null;
        TwoFactorEnabledAt = null;
    }

    /// <summary>
    /// Uses a recovery code to bypass 2FA. The code is removed from the list after use.
    /// </summary>
    /// <param name="code">The recovery code to use.</param>
    /// <returns>True if the code was valid and used, false otherwise.</returns>
    public bool UseRecoveryCode(string code)
    {
        if (!TwoFactorEnabled || string.IsNullOrWhiteSpace(TwoFactorRecoveryCodes))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        // Case-sensitive comparison to maintain full entropy of recovery codes
        var normalizedCode = code.Trim();
        var codes = TwoFactorRecoveryCodes.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList();

        if (!codes.Contains(normalizedCode, StringComparer.Ordinal))
        {
            return false;
        }

        codes.Remove(normalizedCode);
        TwoFactorRecoveryCodes = codes.Count > 0 ? string.Join(",", codes) : null;
        return true;
    }

    /// <summary>
    /// Indicates whether two-factor authentication is enabled and configured.
    /// </summary>
    public bool IsTwoFactorConfigured => TwoFactorEnabled && !string.IsNullOrEmpty(TwoFactorSecretKey);
}
