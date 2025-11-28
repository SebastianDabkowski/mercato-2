using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a user account in the marketplace.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
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
            PasswordHash = string.Empty, // No password for external logins
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
            CreatedAt = DateTime.UtcNow
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
    }

    /// <summary>
    /// Suspends the user account.
    /// </summary>
    public void Suspend()
    {
        Status = UserStatus.Suspended;
    }
}
