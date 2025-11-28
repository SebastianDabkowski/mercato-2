using System.ComponentModel.DataAnnotations;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for the Add Product form.
/// </summary>
public sealed class CreateProductViewModel
{
    [Required(ErrorMessage = "Product title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Product title must be between 3 and 200 characters.")]
    [Display(Name = "Product Title")]
    public string Name { get; set; } = string.Empty;

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
}
