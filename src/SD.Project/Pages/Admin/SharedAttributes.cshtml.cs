using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Page model for managing shared attributes that can be linked across multiple categories.
/// </summary>
[RequireRole(UserRole.Admin)]
public class SharedAttributesModel : PageModel
{
    private readonly ILogger<SharedAttributesModel> _logger;
    private readonly CategoryAttributeService _attributeService;

    public SharedAttributesModel(
        ILogger<SharedAttributesModel> logger,
        CategoryAttributeService attributeService)
    {
        _logger = logger;
        _attributeService = attributeService;
    }

    public IReadOnlyCollection<SharedAttributeViewModel> SharedAttributes { get; private set; } = Array.Empty<SharedAttributeViewModel>();
    public IReadOnlyCollection<SelectListItem> TypeOptions { get; private set; } = Array.Empty<SelectListItem>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public CreateSharedAttributeInput NewAttribute { get; set; } = new();

    [BindProperty]
    public EditSharedAttributeInput EditAttribute { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        await LoadSharedAttributesAsync();
        LoadTypeOptions();

        _logger.LogInformation(
            "Admin {UserId} viewed shared attributes, found {AttributeCount} attributes",
            GetUserId(),
            SharedAttributes.Count);

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
            await LoadSharedAttributesAsync();
            LoadTypeOptions();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var command = new CreateSharedAttributeCommand(
            NewAttribute.Name!,
            NewAttribute.Type,
            NewAttribute.ListValues,
            NewAttribute.Description);

        var result = await _attributeService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} created shared attribute {AttributeId}: {AttributeName}",
                GetUserId(),
                result.Attribute?.Id,
                result.Attribute?.Name);

            return RedirectToPage(new { success = $"Shared attribute '{result.Attribute?.Name}' created successfully." });
        }

        await LoadSharedAttributesAsync();
        LoadTypeOptions();
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
            await LoadSharedAttributesAsync();
            LoadTypeOptions();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var command = new UpdateSharedAttributeCommand(
            EditAttribute.SharedAttributeId,
            EditAttribute.Name!,
            EditAttribute.Type,
            EditAttribute.ListValues,
            EditAttribute.Description);

        var result = await _attributeService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} updated shared attribute {AttributeId}: {AttributeName}",
                GetUserId(),
                result.Attribute?.Id,
                result.Attribute?.Name);

            return RedirectToPage(new { success = result.Message });
        }

        await LoadSharedAttributesAsync();
        LoadTypeOptions();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid sharedAttributeId)
    {
        var command = new DeleteSharedAttributeCommand(sharedAttributeId);
        var result = await _attributeService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} deleted shared attribute {AttributeId}",
                GetUserId(),
                sharedAttributeId);

            return RedirectToPage(new { success = "Shared attribute deleted successfully." });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors) });
    }

    private async Task LoadSharedAttributesAsync()
    {
        var attributes = await _attributeService.HandleAsync(new GetAllSharedAttributesQuery());
        SharedAttributes = attributes
            .Select(a => new SharedAttributeViewModel(
                a.Id,
                a.Name,
                a.Type,
                a.TypeDisplay,
                a.ListValues,
                a.Description,
                a.LinkedCategoryCount,
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

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }

    public class CreateSharedAttributeInput
    {
        [Required(ErrorMessage = "Attribute name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Attribute name must be between 2 and 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Attribute type is required")]
        public AttributeType Type { get; set; }

        [StringLength(1000, ErrorMessage = "List values cannot exceed 1000 characters")]
        public string? ListValues { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public class EditSharedAttributeInput
    {
        [Required]
        public Guid SharedAttributeId { get; set; }

        [Required(ErrorMessage = "Attribute name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Attribute name must be between 2 and 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Attribute type is required")]
        public AttributeType Type { get; set; }

        [StringLength(1000, ErrorMessage = "List values cannot exceed 1000 characters")]
        public string? ListValues { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }
}
