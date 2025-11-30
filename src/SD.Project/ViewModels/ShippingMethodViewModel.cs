using System.ComponentModel.DataAnnotations;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a shipping method in the settings list.
/// </summary>
public sealed class ShippingMethodViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CarrierName { get; set; }
    public int EstimatedDeliveryDaysMin { get; set; }
    public int EstimatedDeliveryDaysMax { get; set; }
    public decimal BaseCost { get; set; }
    public decimal CostPerItem { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public string Currency { get; set; } = "USD";
    public int DisplayOrder { get; set; }
    public string? AvailableRegions { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string DeliveryTimeDisplay
    {
        get
        {
            if (EstimatedDeliveryDaysMin == EstimatedDeliveryDaysMax)
            {
                return EstimatedDeliveryDaysMin == 1
                    ? "1 business day"
                    : $"{EstimatedDeliveryDaysMin} business days";
            }

            return $"{EstimatedDeliveryDaysMin}-{EstimatedDeliveryDaysMax} business days";
        }
    }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";
}

/// <summary>
/// View model for creating or editing a shipping method.
/// </summary>
public sealed class ShippingMethodFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Shipping method name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Carrier name cannot exceed 100 characters.")]
    [Display(Name = "Carrier Name")]
    public string? CarrierName { get; set; }

    [Required(ErrorMessage = "Minimum delivery days is required.")]
    [Range(0, 365, ErrorMessage = "Minimum delivery days must be between 0 and 365.")]
    [Display(Name = "Min. Delivery Days")]
    public int EstimatedDeliveryDaysMin { get; set; } = 1;

    [Required(ErrorMessage = "Maximum delivery days is required.")]
    [Range(0, 365, ErrorMessage = "Maximum delivery days must be between 0 and 365.")]
    [Display(Name = "Max. Delivery Days")]
    public int EstimatedDeliveryDaysMax { get; set; } = 5;

    [Required(ErrorMessage = "Base cost is required.")]
    [Range(0, 10000, ErrorMessage = "Base cost must be between 0 and 10000.")]
    [Display(Name = "Base Cost")]
    public decimal BaseCost { get; set; }

    [Required(ErrorMessage = "Cost per item is required.")]
    [Range(0, 1000, ErrorMessage = "Cost per item must be between 0 and 1000.")]
    [Display(Name = "Cost Per Item")]
    public decimal CostPerItem { get; set; }

    [Range(0, 100000, ErrorMessage = "Free shipping threshold must be between 0 and 100000.")]
    [Display(Name = "Free Shipping Threshold")]
    public decimal? FreeShippingThreshold { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "USD";

    [Range(0, 1000, ErrorMessage = "Display order must be between 0 and 1000.")]
    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [StringLength(200, ErrorMessage = "Available regions cannot exceed 200 characters.")]
    [Display(Name = "Available Regions")]
    public string? AvailableRegions { get; set; }

    [Display(Name = "Set as Default")]
    public bool IsDefault { get; set; }
}
