namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a seller's store profile in the marketplace.
/// </summary>
public class Store
{
    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }

    // Store profile information
    public string Name { get; private set; } = default!;
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }

    // Contact details
    public string ContactEmail { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public string? WebsiteUrl { get; private set; }

    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Store()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new store for the specified seller.
    /// </summary>
    public Store(Guid sellerId, string name, string contactEmail)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("Seller ID is required.", nameof(sellerId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Store name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(contactEmail))
        {
            throw new ArgumentException("Contact email is required.", nameof(contactEmail));
        }

        Id = Guid.NewGuid();
        SellerId = sellerId;
        Name = name.Trim();
        ContactEmail = contactEmail.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the store name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Store name is required.", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the store logo URL.
    /// </summary>
    public void UpdateLogoUrl(string? logoUrl)
    {
        LogoUrl = logoUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the store description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates all contact details.
    /// </summary>
    public void UpdateContactDetails(string contactEmail, string? phoneNumber, string? websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(contactEmail))
        {
            throw new ArgumentException("Contact email is required.", nameof(contactEmail));
        }

        ContactEmail = contactEmail.Trim();
        PhoneNumber = phoneNumber?.Trim();
        WebsiteUrl = websiteUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the full store profile.
    /// </summary>
    public void UpdateProfile(
        string name,
        string? description,
        string contactEmail,
        string? phoneNumber,
        string? websiteUrl)
    {
        UpdateName(name);
        UpdateDescription(description);
        UpdateContactDetails(contactEmail, phoneNumber, websiteUrl);
    }
}
