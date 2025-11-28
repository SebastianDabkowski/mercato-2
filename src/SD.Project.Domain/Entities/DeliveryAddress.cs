using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a delivery address stored for a buyer.
/// Supports multiple addresses per buyer with optional default selection.
/// </summary>
public class DeliveryAddress
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The buyer's user ID. Null for guest checkout addresses stored only on orders.
    /// </summary>
    public Guid? BuyerId { get; private set; }

    /// <summary>
    /// Session identifier for guest checkout addresses.
    /// </summary>
    public string? SessionId { get; private set; }

    /// <summary>
    /// Optional label for the address (e.g., "Home", "Work").
    /// </summary>
    public string? Label { get; private set; }

    /// <summary>
    /// The recipient's full name.
    /// </summary>
    public string RecipientName { get; private set; } = default!;

    /// <summary>
    /// Optional phone number for delivery contact.
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// The address details.
    /// </summary>
    public string Street { get; private set; } = default!;
    public string? Street2 { get; private set; }
    public string City { get; private set; } = default!;
    public string? State { get; private set; }
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;

    /// <summary>
    /// Whether this is the buyer's default delivery address.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Whether this address is still active (not deleted).
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private DeliveryAddress()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new delivery address for a logged-in buyer.
    /// </summary>
    public DeliveryAddress(
        Guid buyerId,
        string recipientName,
        Address address,
        string? phoneNumber = null,
        string? label = null,
        bool isDefault = false)
    {
        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required for authenticated addresses.", nameof(buyerId));
        }

        ValidateRecipientName(recipientName);

        Id = Guid.NewGuid();
        BuyerId = buyerId;
        SessionId = null;
        RecipientName = recipientName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Label = label?.Trim();
        Street = address.Street;
        Street2 = address.Street2;
        City = address.City;
        State = address.State;
        PostalCode = address.PostalCode;
        Country = address.Country;
        IsDefault = isDefault;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new delivery address for a guest checkout (stored temporarily).
    /// </summary>
    public DeliveryAddress(
        string sessionId,
        string recipientName,
        Address address,
        string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required for guest addresses.", nameof(sessionId));
        }

        ValidateRecipientName(recipientName);

        Id = Guid.NewGuid();
        BuyerId = null;
        SessionId = sessionId;
        RecipientName = recipientName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Label = null;
        Street = address.Street;
        Street2 = address.Street2;
        City = address.City;
        State = address.State;
        PostalCode = address.PostalCode;
        Country = address.Country;
        IsDefault = false;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateRecipientName(string recipientName)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new ArgumentException("Recipient name is required.", nameof(recipientName));
        }

        if (recipientName.Length > 200)
        {
            throw new ArgumentException("Recipient name cannot exceed 200 characters.", nameof(recipientName));
        }
    }

    /// <summary>
    /// Updates the address details.
    /// </summary>
    public void Update(
        string recipientName,
        Address address,
        string? phoneNumber = null,
        string? label = null)
    {
        ValidateRecipientName(recipientName);

        RecipientName = recipientName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Label = label?.Trim();
        Street = address.Street;
        Street2 = address.Street2;
        City = address.City;
        State = address.State;
        PostalCode = address.PostalCode;
        Country = address.Country;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets this address as the default for the buyer.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes the default status from this address.
    /// </summary>
    public void RemoveDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft deletes the address.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a previously deactivated address.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Associates a guest address with a buyer account (for cart migration from anonymous to authenticated).
    /// </summary>
    public void AssociateBuyer(Guid buyerId)
    {
        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        BuyerId = buyerId;
        SessionId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the address as an Address value object.
    /// </summary>
    public Address GetAddress()
    {
        return Address.Create(Street, Street2, City, State, PostalCode, Country);
    }
}
