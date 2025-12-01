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

namespace SD.Project.Pages.Admin
{
    [RequireRole(UserRole.Admin)]
    public class CategoriesModel : PageModel
    {
        private readonly ILogger<CategoriesModel> _logger;
        private readonly CategoryService _categoryService;

        public IReadOnlyCollection<CategoryViewModel> Categories { get; private set; } = Array.Empty<CategoryViewModel>();
        public IReadOnlyCollection<SelectListItem> ParentCategoryOptions { get; private set; } = Array.Empty<SelectListItem>();
        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }

        [BindProperty]
        public CreateCategoryInput NewCategory { get; set; } = new();

        [BindProperty]
        public EditCategoryInput EditCategory { get; set; } = new();

        public CategoriesModel(
            ILogger<CategoriesModel> logger,
            CategoryService categoryService)
        {
            _logger = logger;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
        {
            SuccessMessage = success;
            ErrorMessage = error;

            await LoadCategoriesAsync();

            _logger.LogInformation(
                "Admin {UserId} viewed categories, found {CategoryCount} categories",
                GetUserId(),
                Categories.Count);

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new CreateCategoryCommand(
                NewCategory.Name!,
                NewCategory.ParentId,
                NewCategory.DisplayOrder,
                NewCategory.Description,
                NewCategory.Slug);

            var result = await _categoryService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} created category {CategoryId}: {CategoryName}",
                    GetUserId(),
                    result.Category?.Id,
                    result.Category?.Name);

                return RedirectToPage(new { success = $"Category '{result.Category?.Name}' created successfully." });
            }

            await LoadCategoriesAsync();
            ErrorMessage = string.Join(" ", result.Errors);
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new UpdateCategoryCommand(
                EditCategory.CategoryId,
                EditCategory.Name!,
                EditCategory.ParentId,
                EditCategory.DisplayOrder,
                EditCategory.Description,
                EditCategory.Slug);

            var result = await _categoryService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} updated category {CategoryId}: {CategoryName}",
                    GetUserId(),
                    result.Category?.Id,
                    result.Category?.Name);

                return RedirectToPage(new { success = $"Category '{result.Category?.Name}' updated successfully." });
            }

            await LoadCategoriesAsync();
            ErrorMessage = string.Join(" ", result.Errors);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid categoryId)
        {
            var command = new DeleteCategoryCommand(categoryId);
            var result = await _categoryService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} deleted category {CategoryId}",
                    GetUserId(),
                    categoryId);

                return RedirectToPage(new { success = "Category deleted successfully." });
            }

            return RedirectToPage(new { error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(Guid categoryId)
        {
            var command = new ToggleCategoryStatusCommand(categoryId);
            var result = await _categoryService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} toggled status for category {CategoryId} to {IsActive}",
                    GetUserId(),
                    categoryId,
                    result.Category?.IsActive);

                return RedirectToPage(new { success = result.Message });
            }

            return RedirectToPage(new { error = string.Join(" ", result.Errors) });
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _categoryService.HandleAsync(new GetAllCategoriesQuery());

            // Build tree-structured list for display
            var viewModels = new List<CategoryViewModel>();
            BuildCategoryTree(categories, null, 0, viewModels);
            Categories = viewModels.AsReadOnly();

            // Build parent options for dropdowns
            var options = new List<SelectListItem>
            {
                new SelectListItem("(Root - No Parent)", "")
            };
            AddCategoryOptions(categories, null, 0, options);
            ParentCategoryOptions = options.AsReadOnly();
        }

        private void BuildCategoryTree(
            IReadOnlyCollection<CategoryDto> allCategories,
            Guid? parentId,
            int level,
            List<CategoryViewModel> result)
        {
            var children = allCategories
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

            foreach (var category in children)
            {
                result.Add(new CategoryViewModel(
                    category.Id,
                    category.Name,
                    category.Description,
                    category.Slug,
                    category.ParentId,
                    category.ParentName,
                    category.DisplayOrder,
                    category.IsActive,
                    category.CreatedAt,
                    category.UpdatedAt,
                    category.ProductCount,
                    category.ChildCount)
                {
                    Level = level
                });

                BuildCategoryTree(allCategories, category.Id, level + 1, result);
            }
        }

        private void AddCategoryOptions(
            IReadOnlyCollection<CategoryDto> allCategories,
            Guid? parentId,
            int level,
            List<SelectListItem> options)
        {
            var children = allCategories
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

            foreach (var category in children)
            {
                var indent = level > 0 ? new string('—', level) + " " : "";
                options.Add(new SelectListItem(indent + category.Name, category.Id.ToString()));
                AddCategoryOptions(allCategories, category.Id, level + 1, options);
            }
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        }

        public class CreateCategoryInput
        {
            [Required(ErrorMessage = "Category name is required")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
            public string? Name { get; set; }

            [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
            public string? Description { get; set; }

            [StringLength(100, ErrorMessage = "Slug cannot exceed 100 characters")]
            [RegularExpression(@"^[a-z0-9\-]*$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
            public string? Slug { get; set; }

            public Guid? ParentId { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Display order must be a non-negative number")]
            public int DisplayOrder { get; set; }
        }

        public class EditCategoryInput
        {
            [Required]
            public Guid CategoryId { get; set; }

            [Required(ErrorMessage = "Category name is required")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
            public string? Name { get; set; }

            [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
            public string? Description { get; set; }

            [StringLength(100, ErrorMessage = "Slug cannot exceed 100 characters")]
            [RegularExpression(@"^[a-z0-9\-]*$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
            public string? Slug { get; set; }

            public Guid? ParentId { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Display order must be a non-negative number")]
            public int DisplayOrder { get; set; }
        }
    }
}
