using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Page model for managing category attribute templates.
/// </summary>
[RequireRole(UserRole.Admin)]
public class CategoryAttributesModel : PageModel
{
    private readonly ILogger<CategoryAttributesModel> _logger;
    private readonly CategoryAttributeService _attributeService;
    private readonly CategoryService _categoryService;

    public CategoryAttributesModel(
        ILogger<CategoryAttributesModel> logger,
        CategoryAttributeService attributeService,
        CategoryService categoryService)
    {
        _logger = logger;
        _attributeService = attributeService;
        _categoryService = categoryService;
    }

    public CategoryViewModel? Category { get; private set; }
    public IReadOnlyCollection<CategoryAttributeViewModel> Attributes { get; private set; } = Array.Empty<CategoryAttributeViewModel>();
    public IReadOnlyCollection<SelectListItem> TypeOptions { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> SharedAttributeOptions { get; private set; } = Array.Empty<SelectListItem>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid CategoryId { get; set; }

    [BindProperty]
    public CreateAttributeInput NewAttribute { get; set; } = new();

    [BindProperty]
    public EditAttributeInput EditAttribute { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        var categoryDto = await _categoryService.HandleAsync(new GetCategoryByIdQuery(CategoryId));
        if (categoryDto is null)
        {
            return NotFound();
        }

        Category = MapCategoryToViewModel(categoryDto);
        await LoadAttributesAsync();
        LoadTypeOptions();
        await LoadSharedAttributeOptionsAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed attributes for category {CategoryId}: {CategoryName}, found {AttributeCount} attributes",
            GetUserId(),
            CategoryId,
            Category.Name,
            Attributes.Count);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        // Only validate ListValues if type is List
        if (NewAttribute.Type != AttributeType.List)
        {
            ModelState.Remove($"{nameof(NewAttribute)}.{nameof(NewAttribute.ListValues)}");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var command = new CreateCategoryAttributeCommand(
            CategoryId,
            NewAttribute.Name!,
            NewAttribute.Type,
            NewAttribute.IsRequired,
            NewAttribute.ListValues,
            NewAttribute.DisplayOrder,
            NewAttribute.SharedAttributeId);

        var result = await _attributeService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} created attribute {AttributeId}: {AttributeName} for category {CategoryId}",
                GetUserId(),
                result.Attribute?.Id,
                result.Attribute?.Name,
                CategoryId);

            return RedirectToPage(new { CategoryId, success = $"Attribute '{result.Attribute?.Name}' created successfully." });
        }

        await LoadPageDataAsync();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        // Only validate ListValues if type is List
        if (EditAttribute.Type != AttributeType.List)
        {
            ModelState.Remove($"{nameof(EditAttribute)}.{nameof(EditAttribute.ListValues)}");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var command = new UpdateCategoryAttributeCommand(
            EditAttribute.AttributeId,
            EditAttribute.Name!,
            EditAttribute.Type,
            EditAttribute.IsRequired,
            EditAttribute.ListValues,
            EditAttribute.DisplayOrder);

        var result = await _attributeService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} updated attribute {AttributeId}: {AttributeName}",
                GetUserId(),
                result.Attribute?.Id,
                result.Attribute?.Name);

            return RedirectToPage(new { CategoryId, success = $"Attribute '{result.Attribute?.Name}' updated successfully." });
        }

        await LoadPageDataAsync();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid attributeId)
    {
        var command = new DeleteCategoryAttributeCommand(attributeId);
        var result = await _attributeService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} deleted attribute {AttributeId}",
                GetUserId(),
                attributeId);

            return RedirectToPage(new { CategoryId, success = "Attribute deleted successfully." });
        }

        return RedirectToPage(new { CategoryId, error = string.Join(" ", result.Errors) });
    }

    public async Task<IActionResult> OnPostDeprecateAsync(Guid attributeId)
    {
        var command = new DeprecateCategoryAttributeCommand(attributeId);
        var result = await _attributeService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} deprecated attribute {AttributeId}",
                GetUserId(),
                attributeId);

            return RedirectToPage(new { CategoryId, success = result.Message });
        }

        return RedirectToPage(new { CategoryId, error = string.Join(" ", result.Errors) });
    }

    public async Task<IActionResult> OnPostReactivateAsync(Guid attributeId)
    {
        var command = new ReactivateCategoryAttributeCommand(attributeId);
        var result = await _attributeService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} reactivated attribute {AttributeId}",
                GetUserId(),
                attributeId);

            return RedirectToPage(new { CategoryId, success = result.Message });
        }

        return RedirectToPage(new { CategoryId, error = string.Join(" ", result.Errors) });
    }

    private async Task LoadPageDataAsync()
    {
        var categoryDto = await _categoryService.HandleAsync(new GetCategoryByIdQuery(CategoryId));
        if (categoryDto is not null)
        {
            Category = MapCategoryToViewModel(categoryDto);
        }

        await LoadAttributesAsync();
        LoadTypeOptions();
        await LoadSharedAttributeOptionsAsync();
    }

    private async Task LoadAttributesAsync()
    {
        var attributes = await _attributeService.HandleAsync(new GetCategoryAttributesQuery(CategoryId, IncludeDeprecated: true));
        Attributes = attributes
            .Select(a => new CategoryAttributeViewModel(
                a.Id,
                a.CategoryId,
                a.Name,
                a.Type,
                a.TypeDisplay,
                a.IsRequired,
                a.ListValues,
                a.DisplayOrder,
                a.IsDeprecated,
                a.SharedAttributeId,
                a.SharedAttributeName,
                a.CreatedAt,
                a.UpdatedAt))
            .ToList()
            .AsReadOnly();
    }

    private void LoadTypeOptions()
    {
        TypeOptions = new List<SelectListItem>
        {
            new SelectListItem("Text", ((int)AttributeType.Text).ToString()),
            new SelectListItem("Number", ((int)AttributeType.Number).ToString()),
            new SelectListItem("List", ((int)AttributeType.List).ToString()),
            new SelectListItem("Yes/No", ((int)AttributeType.Boolean).ToString()),
            new SelectListItem("Date", ((int)AttributeType.Date).ToString())
        }.AsReadOnly();
    }

    private async Task LoadSharedAttributeOptionsAsync()
    {
        var sharedAttributes = await _attributeService.HandleAsync(new GetAllSharedAttributesQuery());
        var options = new List<SelectListItem>
        {
            new SelectListItem("(No shared attribute)", "")
        };
        options.AddRange(sharedAttributes.Select(a => new SelectListItem(a.Name, a.Id.ToString())));
        SharedAttributeOptions = options.AsReadOnly();
    }

    private static CategoryViewModel MapCategoryToViewModel(CategoryDto dto)
    {
        return new CategoryViewModel(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.Slug,
            dto.ParentId,
            dto.ParentName,
            dto.DisplayOrder,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.ProductCount,
            dto.ChildCount);
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }

    public class CreateAttributeInput
    {
        [Required(ErrorMessage = "Attribute name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Attribute name must be between 2 and 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Attribute type is required")]
        public AttributeType Type { get; set; }

        public bool IsRequired { get; set; }

        [StringLength(1000, ErrorMessage = "List values cannot exceed 1000 characters")]
        public string? ListValues { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; }

        public Guid? SharedAttributeId { get; set; }
    }

    public class EditAttributeInput
    {
        [Required]
        public Guid AttributeId { get; set; }

        [Required(ErrorMessage = "Attribute name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Attribute name must be between 2 and 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Attribute type is required")]
        public AttributeType Type { get; set; }

        public bool IsRequired { get; set; }

        [StringLength(1000, ErrorMessage = "List values cannot exceed 1000 characters")]
        public string? ListValues { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; }
    }
}
