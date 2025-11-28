using System.ComponentModel.DataAnnotations;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for the store settings page.
/// </summary>
public sealed class StoreSettingsViewModel
{
    [Required(ErrorMessage = "Store name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Store name must be between 3 and 100 characters.")]
    [Display(Name = "Store Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Store Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Contact email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL.")]
    [Display(Name = "Website URL")]
    public string? WebsiteUrl { get; set; }

    public string? LogoUrl { get; set; }
}
