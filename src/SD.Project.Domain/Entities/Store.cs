using System.Text.RegularExpressions;

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
    public string Slug { get; private set; } = default!;
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }

    // Store status
    public StoreStatus Status { get; private set; }

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
        Slug = GenerateSlug(name);
        ContactEmail = contactEmail.Trim();
        Status = StoreStatus.PendingVerification;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates a URL-friendly slug from the given name.
    /// </summary>
    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty when generating slug.", nameof(name));
        }

        // Convert to lowercase and replace spaces with hyphens
        var slug = name.Trim().ToLowerInvariant();
        
        // Replace spaces and underscores with hyphens
        slug = Regex.Replace(slug, @"[\s_]+", "-");
        
        // Remove non-alphanumeric characters except hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        
        // Remove consecutive hyphens
        slug = Regex.Replace(slug, @"-+", "-");
        
        // Trim hyphens from start and end
        slug = slug.Trim('-');

        // If slug is empty after processing (e.g., name was only special chars), use a fallback
        if (string.IsNullOrEmpty(slug))
        {
            slug = "store";
        }

        return slug;
    }

    /// <summary>
    /// Creates a slug from the given name without updating the store.
    /// Useful for checking slug uniqueness before updating.
    /// </summary>
    public static string CreateSlugFromName(string name)
    {
        return GenerateSlug(name);
    }

    /// <summary>
    /// Updates the store name and regenerates the slug.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Store name is required.", nameof(name));
        }

        Name = name.Trim();
        Slug = GenerateSlug(name);
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

    /// <summary>
    /// Updates the store status.
    /// </summary>
    public void UpdateStatus(StoreStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the store after verification.
    /// </summary>
    public void Activate()
    {
        Status = StoreStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspends the store.
    /// </summary>
    public void Suspend()
    {
        Status = StoreStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if the store is publicly visible.
    /// Active and LimitedActive stores are publicly visible.
    /// </summary>
    public bool IsPubliclyVisible()
    {
        return Status == StoreStatus.Active || Status == StoreStatus.LimitedActive;
    }

    /// <summary>
    /// Deactivates the store when the seller's account is deleted.
    /// </summary>
    public void Deactivate()
    {
        Status = StoreStatus.Deactivated;
        UpdatedAt = DateTime.UtcNow;
    }
}
