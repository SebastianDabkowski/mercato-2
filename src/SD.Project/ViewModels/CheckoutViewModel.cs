using System.ComponentModel.DataAnnotations;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a delivery address.
/// </summary>
public sealed record DeliveryAddressViewModel(
    Guid Id,
    string RecipientName,
    string? PhoneNumber,
    string? Label,
    string Street,
    string? Street2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    bool IsDefault,
    string FormattedAddress);

/// <summary>
/// View model for the checkout address form input.
/// </summary>
public class CheckoutAddressInputModel
{
    public Guid? SelectedAddressId { get; set; }

    [Required(ErrorMessage = "Recipient name is required.")]
    [StringLength(200, ErrorMessage = "Recipient name cannot exceed 200 characters.")]
    public string RecipientName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
    public string? PhoneNumber { get; set; }

    [StringLength(50, ErrorMessage = "Label cannot exceed 50 characters.")]
    public string? Label { get; set; }

    [Required(ErrorMessage = "Street address is required.")]
    [StringLength(200, ErrorMessage = "Street address cannot exceed 200 characters.")]
    public string Street { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Street address line 2 cannot exceed 200 characters.")]
    public string? Street2 { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string City { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "State/Province cannot exceed 100 characters.")]
    public string? State { get; set; }

    [Required(ErrorMessage = "Postal code is required.")]
    [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters.")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required.")]
    [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
    public string Country { get; set; } = string.Empty;

    public bool SetAsDefault { get; set; }

    public bool SaveToProfile { get; set; } = true;
}

/// <summary>
/// View model for the checkout page showing the address step.
/// </summary>
public sealed record CheckoutViewModel(
    IReadOnlyList<DeliveryAddressViewModel> SavedAddresses,
    DeliveryAddressViewModel? SelectedAddress,
    CartSummaryViewModel CartSummary,
    bool IsAuthenticated);

/// <summary>
/// Summary of cart for checkout display.
/// </summary>
public sealed record CartSummaryViewModel(
    int TotalItemCount,
    decimal Subtotal,
    decimal ShippingTotal,
    decimal TotalAmount,
    string Currency);
