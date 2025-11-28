using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating category use cases.
/// </summary>
public sealed class CategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Handles a request to create a category.
    /// </summary>
    public async Task<CreateCategoryResultDto> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateCategory(command.Name);
        if (validationErrors.Count > 0)
        {
            return CreateCategoryResultDto.Failed(validationErrors);
        }

        // Validate parent exists if specified
        if (command.ParentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(command.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                return CreateCategoryResultDto.Failed("Parent category not found.");
            }
        }

        try
        {
            var category = new Category(
                Guid.NewGuid(),
                command.Name,
                command.ParentId,
                command.DisplayOrder);

            await _repository.AddAsync(category, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var dto = await MapToDtoAsync(category, cancellationToken);
            return CreateCategoryResultDto.Succeeded(dto);
        }
        catch (ArgumentException ex)
        {
            return CreateCategoryResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to update an existing category.
    /// </summary>
    public async Task<UpdateCategoryResultDto> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await _repository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return UpdateCategoryResultDto.Failed("Category not found.");
        }

        var validationErrors = ValidateCategory(command.Name);
        if (validationErrors.Count > 0)
        {
            return UpdateCategoryResultDto.Failed(validationErrors);
        }

        // Validate parent exists if specified and prevent circular references
        if (command.ParentId.HasValue)
        {
            if (command.ParentId.Value == command.CategoryId)
            {
                return UpdateCategoryResultDto.Failed("A category cannot be its own parent.");
            }

            var parent = await _repository.GetByIdAsync(command.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                return UpdateCategoryResultDto.Failed("Parent category not found.");
            }

            // Check for circular reference (parent cannot be a descendant of this category)
            if (await IsDescendantAsync(command.ParentId.Value, command.CategoryId, cancellationToken))
            {
                return UpdateCategoryResultDto.Failed("Cannot set a descendant category as parent (circular reference).");
            }
        }

        try
        {
            category.UpdateName(command.Name);
            category.UpdateParent(command.ParentId);
            category.UpdateDisplayOrder(command.DisplayOrder);

            _repository.Update(category);
            await _repository.SaveChangesAsync(cancellationToken);

            var dto = await MapToDtoAsync(category, cancellationToken);
            return UpdateCategoryResultDto.Succeeded(dto);
        }
        catch (ArgumentException ex)
        {
            return UpdateCategoryResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to delete a category.
    /// </summary>
    public async Task<DeleteCategoryResultDto> HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await _repository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return DeleteCategoryResultDto.Failed("Category not found.");
        }

        // Check if category has children
        var hasChildren = await _repository.HasChildrenAsync(command.CategoryId, cancellationToken);
        if (hasChildren)
        {
            return DeleteCategoryResultDto.Failed("Cannot delete a category that has subcategories. Please delete or reassign subcategories first.");
        }

        // Check if category has assigned products
        var productCount = await _repository.GetProductCountAsync(command.CategoryId, cancellationToken);
        if (productCount > 0)
        {
            return DeleteCategoryResultDto.Failed($"Cannot delete a category with {productCount} assigned product(s). Please reassign products to another category first, or deactivate the category instead.");
        }

        try
        {
            _repository.Delete(category);
            await _repository.SaveChangesAsync(cancellationToken);

            return DeleteCategoryResultDto.Succeeded();
        }
        catch (Exception ex)
        {
            return DeleteCategoryResultDto.Failed($"Failed to delete category: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles a request to toggle category active status.
    /// </summary>
    public async Task<UpdateCategoryResultDto> HandleAsync(ToggleCategoryStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await _repository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return UpdateCategoryResultDto.Failed("Category not found.");
        }

        try
        {
            if (category.IsActive)
            {
                category.Deactivate();
            }
            else
            {
                category.Activate();
            }

            _repository.Update(category);
            await _repository.SaveChangesAsync(cancellationToken);

            var dto = await MapToDtoAsync(category, cancellationToken);
            var message = category.IsActive ? "Category activated successfully." : "Category deactivated successfully.";
            return UpdateCategoryResultDto.Succeeded(dto, message);
        }
        catch (ArgumentException ex)
        {
            return UpdateCategoryResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves all categories.
    /// </summary>
    public async Task<IReadOnlyCollection<CategoryDto>> HandleAsync(GetAllCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var categories = await _repository.GetAllAsync(cancellationToken);
        var result = new List<CategoryDto>();

        foreach (var category in categories)
        {
            result.Add(await MapToDtoAsync(category, cancellationToken));
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Retrieves only active categories.
    /// </summary>
    public async Task<IReadOnlyCollection<CategoryDto>> HandleAsync(GetActiveCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var categories = await _repository.GetActiveAsync(cancellationToken);
        var result = new List<CategoryDto>();

        foreach (var category in categories)
        {
            result.Add(await MapToDtoAsync(category, cancellationToken));
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Retrieves a single category by its ID.
    /// </summary>
    public async Task<CategoryDto?> HandleAsync(GetCategoryByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var category = await _repository.GetByIdAsync(query.CategoryId, cancellationToken);
        return category is null ? null : await MapToDtoAsync(category, cancellationToken);
    }

    private async Task<CategoryDto> MapToDtoAsync(Category category, CancellationToken cancellationToken)
    {
        string? parentName = null;
        if (category.ParentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(category.ParentId.Value, cancellationToken);
            parentName = parent?.Name;
        }

        var productCount = await _repository.GetProductCountAsync(category.Id, cancellationToken);
        var children = await _repository.GetChildrenAsync(category.Id, cancellationToken);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.ParentId,
            parentName,
            category.DisplayOrder,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt,
            productCount,
            children.Count);
    }

    private async Task<bool> IsDescendantAsync(Guid potentialDescendantId, Guid ancestorId, CancellationToken cancellationToken)
    {
        var children = await _repository.GetChildrenAsync(ancestorId, cancellationToken);
        foreach (var child in children)
        {
            if (child.Id == potentialDescendantId)
            {
                return true;
            }

            if (await IsDescendantAsync(potentialDescendantId, child.Id, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> ValidateCategory(string name)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Category name is required.");
        }
        else if (name.Trim().Length < 2)
        {
            errors.Add("Category name must be at least 2 characters long.");
        }
        else if (name.Trim().Length > 100)
        {
            errors.Add("Category name cannot exceed 100 characters.");
        }

        return errors;
    }
}
