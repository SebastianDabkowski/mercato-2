using System.ComponentModel.DataAnnotations;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for the Edit Product form.
/// </summary>
public sealed class EditProductViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Product title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Product title must be between 3 and 200 characters.")]
    [Display(Name = "Product Title")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99.")]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "USD";

    [Required(ErrorMessage = "Stock quantity is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    [Display(Name = "Stock Quantity")]
    public int Stock { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Category cannot exceed 100 characters.")]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "Weight must be between 0 and 10,000 kg.")]
    [Display(Name = "Weight (kg)")]
    public decimal? WeightKg { get; set; }

    [Range(0, 10000, ErrorMessage = "Length must be between 0 and 10,000 cm.")]
    [Display(Name = "Length (cm)")]
    public decimal? LengthCm { get; set; }

    [Range(0, 10000, ErrorMessage = "Width must be between 0 and 10,000 cm.")]
    [Display(Name = "Width (cm)")]
    public decimal? WidthCm { get; set; }

    [Range(0, 10000, ErrorMessage = "Height must be between 0 and 10,000 cm.")]
    [Display(Name = "Height (cm)")]
    public decimal? HeightCm { get; set; }
}
