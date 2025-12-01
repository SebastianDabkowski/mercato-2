using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating category attribute use cases.
/// </summary>
public sealed class CategoryAttributeService
{
    private readonly ICategoryAttributeRepository _attributeRepository;
    private readonly ISharedAttributeRepository _sharedAttributeRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CategoryAttributeService(
        ICategoryAttributeRepository attributeRepository,
        ISharedAttributeRepository sharedAttributeRepository,
        ICategoryRepository categoryRepository)
    {
        _attributeRepository = attributeRepository;
        _sharedAttributeRepository = sharedAttributeRepository;
        _categoryRepository = categoryRepository;
    }

    /// <summary>
    /// Handles a request to create a category attribute.
    /// </summary>
    public async Task<CreateCategoryAttributeResultDto> HandleAsync(CreateCategoryAttributeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateAttribute(command.Name, command.Type, command.ListValues);
        if (validationErrors.Count > 0)
        {
            return CreateCategoryAttributeResultDto.Failed(validationErrors);
        }

        // Validate category exists
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return CreateCategoryAttributeResultDto.Failed("Category not found.");
        }

        // Check for duplicate name in category
        var nameExists = await _attributeRepository.ExistsByNameAsync(command.CategoryId, command.Name, cancellationToken: cancellationToken);
        if (nameExists)
        {
            return CreateCategoryAttributeResultDto.Failed($"An attribute with the name '{command.Name}' already exists for this category.");
        }

        // Validate shared attribute if specified
        SharedAttribute? sharedAttribute = null;
        if (command.SharedAttributeId.HasValue)
        {
            sharedAttribute = await _sharedAttributeRepository.GetByIdAsync(command.SharedAttributeId.Value, cancellationToken);
            if (sharedAttribute is null)
            {
                return CreateCategoryAttributeResultDto.Failed("Shared attribute not found.");
            }
        }

        try
        {
            var attribute = new CategoryAttribute(
                Guid.NewGuid(),
                command.CategoryId,
                command.Name,
                command.Type,
                command.IsRequired,
                command.ListValues,
                command.DisplayOrder,
                command.SharedAttributeId);

            await _attributeRepository.AddAsync(attribute, cancellationToken);
            await _attributeRepository.SaveChangesAsync(cancellationToken);

            var dto = MapToDto(attribute, sharedAttribute?.Name);
            return CreateCategoryAttributeResultDto.Succeeded(dto);
        }
        catch (ArgumentException ex)
        {
            return CreateCategoryAttributeResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to update a category attribute.
    /// </summary>
    public async Task<UpdateCategoryAttributeResultDto> HandleAsync(UpdateCategoryAttributeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attribute = await _attributeRepository.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return UpdateCategoryAttributeResultDto.Failed("Attribute not found.");
        }

        var validationErrors = ValidateAttribute(command.Name, command.Type, command.ListValues);
        if (validationErrors.Count > 0)
        {
            return UpdateCategoryAttributeResultDto.Failed(validationErrors);
        }

        // Check for duplicate name (excluding current attribute)
        var nameExists = await _attributeRepository.ExistsByNameAsync(attribute.CategoryId, command.Name, command.AttributeId, cancellationToken);
        if (nameExists)
        {
            return UpdateCategoryAttributeResultDto.Failed($"An attribute with the name '{command.Name}' already exists for this category.");
        }

        try
        {
            attribute.UpdateName(command.Name);
            attribute.UpdateType(command.Type, command.ListValues);
            attribute.UpdateRequired(command.IsRequired);
            attribute.UpdateDisplayOrder(command.DisplayOrder);

            _attributeRepository.Update(attribute);
            await _attributeRepository.SaveChangesAsync(cancellationToken);

            string? sharedAttributeName = null;
            if (attribute.SharedAttributeId.HasValue)
            {
                var sharedAttribute = await _sharedAttributeRepository.GetByIdAsync(attribute.SharedAttributeId.Value, cancellationToken);
                sharedAttributeName = sharedAttribute?.Name;
            }

            var dto = MapToDto(attribute, sharedAttributeName);
            return UpdateCategoryAttributeResultDto.Succeeded(dto);
        }
        catch (ArgumentException ex)
        {
            return UpdateCategoryAttributeResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to delete a category attribute.
    /// </summary>
    public async Task<DeleteCategoryAttributeResultDto> HandleAsync(DeleteCategoryAttributeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attribute = await _attributeRepository.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return DeleteCategoryAttributeResultDto.Failed("Attribute not found.");
        }

        try
        {
            _attributeRepository.Delete(attribute);
            await _attributeRepository.SaveChangesAsync(cancellationToken);

            return DeleteCategoryAttributeResultDto.Succeeded();
        }
        catch (Exception ex)
        {
            return DeleteCategoryAttributeResultDto.Failed($"Failed to delete attribute: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles a request to deprecate a category attribute.
    /// </summary>
    public async Task<UpdateCategoryAttributeResultDto> HandleAsync(DeprecateCategoryAttributeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attribute = await _attributeRepository.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return UpdateCategoryAttributeResultDto.Failed("Attribute not found.");
        }

        if (attribute.IsDeprecated)
        {
            return UpdateCategoryAttributeResultDto.Failed("Attribute is already deprecated.");
        }

        try
        {
            attribute.Deprecate();
            _attributeRepository.Update(attribute);
            await _attributeRepository.SaveChangesAsync(cancellationToken);

            string? sharedAttributeName = null;
            if (attribute.SharedAttributeId.HasValue)
            {
                var sharedAttribute = await _sharedAttributeRepository.GetByIdAsync(attribute.SharedAttributeId.Value, cancellationToken);
                sharedAttributeName = sharedAttribute?.Name;
            }

            var dto = MapToDto(attribute, sharedAttributeName);
            return UpdateCategoryAttributeResultDto.Succeeded(dto, "Attribute deprecated successfully.");
        }
        catch (ArgumentException ex)
        {
            return UpdateCategoryAttributeResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to reactivate a deprecated category attribute.
    /// </summary>
    public async Task<UpdateCategoryAttributeResultDto> HandleAsync(ReactivateCategoryAttributeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attribute = await _attributeRepository.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return UpdateCategoryAttributeResultDto.Failed("Attribute not found.");
        }

        if (!attribute.IsDeprecated)
        {
            return UpdateCategoryAttributeResultDto.Failed("Attribute is not deprecated.");
        }

        try
        {
            attribute.Reactivate();
            _attributeRepository.Update(attribute);
            await _attributeRepository.SaveChangesAsync(cancellationToken);

            string? sharedAttributeName = null;
            if (attribute.SharedAttributeId.HasValue)
            {
                var sharedAttribute = await _sharedAttributeRepository.GetByIdAsync(attribute.SharedAttributeId.Value, cancellationToken);
                sharedAttributeName = sharedAttribute?.Name;
            }

            var dto = MapToDto(attribute, sharedAttributeName);
            return UpdateCategoryAttributeResultDto.Succeeded(dto, "Attribute reactivated successfully.");
        }
        catch (ArgumentException ex)
        {
            return UpdateCategoryAttributeResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves all attributes for a category.
    /// </summary>
    public async Task<IReadOnlyCollection<CategoryAttributeDto>> HandleAsync(GetCategoryAttributesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var attributes = query.IncludeDeprecated
            ? await _attributeRepository.GetByCategoryIdAsync(query.CategoryId, cancellationToken)
            : await _attributeRepository.GetActiveByCategoryIdAsync(query.CategoryId, cancellationToken);

        return await MapToDtosAsync(attributes, cancellationToken);
    }

    /// <summary>
    /// Retrieves a specific category attribute by ID.
    /// </summary>
    public async Task<CategoryAttributeDto?> HandleAsync(GetCategoryAttributeByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var attribute = await _attributeRepository.GetByIdAsync(query.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return null;
        }

        string? sharedAttributeName = null;
        if (attribute.SharedAttributeId.HasValue)
        {
            var sharedAttribute = await _sharedAttributeRepository.GetByIdAsync(attribute.SharedAttributeId.Value, cancellationToken);
            sharedAttributeName = sharedAttribute?.Name;
        }

        return MapToDto(attribute, sharedAttributeName);
    }

    /// <summary>
    /// Handles a request to create a shared attribute.
    /// </summary>
    public async Task<CreateSharedAttributeResultDto> HandleAsync(CreateSharedAttributeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateAttribute(command.Name, command.Type, command.ListValues);
        if (validationErrors.Count > 0)
        {
            return CreateSharedAttributeResultDto.Failed(validationErrors);
        }

        // Check for duplicate name
        var nameExists = await _sharedAttributeRepository.ExistsByNameAsync(command.Name, cancellationToken: cancellationToken);
        if (nameExists)
        {
            return CreateSharedAttributeResultDto.Failed($"A shared attribute with the name '{command.Name}' already exists.");
        }

        try
        {
            var attribute = new SharedAttribute(
                Guid.NewGuid(),
                command.Name,
                command.Type,
                command.ListValues,
                command.Description);

            await _sharedAttributeRepository.AddAsync(attribute, cancellationToken);
            await _sharedAttributeRepository.SaveChangesAsync(cancellationToken);

            var dto = MapToDto(attribute, 0);
            return CreateSharedAttributeResultDto.Succeeded(dto);
        }
        catch (ArgumentException ex)
        {
            return CreateSharedAttributeResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to update a shared attribute.
    /// Changes are reflected in all linked category attributes.
    /// </summary>
    public async Task<UpdateSharedAttributeResultDto> HandleAsync(UpdateSharedAttributeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sharedAttribute = await _sharedAttributeRepository.GetByIdAsync(command.SharedAttributeId, cancellationToken);
        if (sharedAttribute is null)
        {
            return UpdateSharedAttributeResultDto.Failed("Shared attribute not found.");
        }

        var validationErrors = ValidateAttribute(command.Name, command.Type, command.ListValues);
        if (validationErrors.Count > 0)
        {
            return UpdateSharedAttributeResultDto.Failed(validationErrors);
        }

        // Check for duplicate name (excluding current)
        var nameExists = await _sharedAttributeRepository.ExistsByNameAsync(command.Name, command.SharedAttributeId, cancellationToken);
        if (nameExists)
        {
            return UpdateSharedAttributeResultDto.Failed($"A shared attribute with the name '{command.Name}' already exists.");
        }

        try
        {
            sharedAttribute.UpdateName(command.Name);
            sharedAttribute.UpdateType(command.Type, command.ListValues);
            sharedAttribute.UpdateDescription(command.Description);

            _sharedAttributeRepository.Update(sharedAttribute);

            // Update all linked category attributes
            var linkedAttributes = await _attributeRepository.GetBySharedAttributeIdAsync(command.SharedAttributeId, cancellationToken);
            foreach (var linkedAttribute in linkedAttributes)
            {
                linkedAttribute.UpdateName(command.Name);
                linkedAttribute.UpdateType(command.Type, command.ListValues);
                _attributeRepository.Update(linkedAttribute);
            }

            await _sharedAttributeRepository.SaveChangesAsync(cancellationToken);

            var linkedCount = await _sharedAttributeRepository.GetLinkedCategoryCountAsync(command.SharedAttributeId, cancellationToken);
            var dto = MapToDto(sharedAttribute, linkedCount);
            var message = linkedAttributes.Count > 0 
                ? $"Shared attribute updated and {linkedAttributes.Count} linked category attribute(s) synchronized." 
                : "Shared attribute updated successfully.";
            return UpdateSharedAttributeResultDto.Succeeded(dto, message);
        }
        catch (ArgumentException ex)
        {
            return UpdateSharedAttributeResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to delete a shared attribute.
    /// </summary>
    public async Task<DeleteSharedAttributeResultDto> HandleAsync(DeleteSharedAttributeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sharedAttribute = await _sharedAttributeRepository.GetByIdAsync(command.SharedAttributeId, cancellationToken);
        if (sharedAttribute is null)
        {
            return DeleteSharedAttributeResultDto.Failed("Shared attribute not found.");
        }

        // Check if any category attributes are linked
        var linkedCount = await _sharedAttributeRepository.GetLinkedCategoryCountAsync(command.SharedAttributeId, cancellationToken);
        if (linkedCount > 0)
        {
            return DeleteSharedAttributeResultDto.Failed($"Cannot delete shared attribute that is linked to {linkedCount} category attribute(s). Unlink them first.");
        }

        try
        {
            _sharedAttributeRepository.Delete(sharedAttribute);
            await _sharedAttributeRepository.SaveChangesAsync(cancellationToken);

            return DeleteSharedAttributeResultDto.Succeeded();
        }
        catch (Exception ex)
        {
            return DeleteSharedAttributeResultDto.Failed($"Failed to delete shared attribute: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves all shared attributes.
    /// </summary>
    public async Task<IReadOnlyCollection<SharedAttributeDto>> HandleAsync(GetAllSharedAttributesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sharedAttributes = await _sharedAttributeRepository.GetAllAsync(cancellationToken);
        var result = new List<SharedAttributeDto>();

        foreach (var attr in sharedAttributes)
        {
            var linkedCount = await _sharedAttributeRepository.GetLinkedCategoryCountAsync(attr.Id, cancellationToken);
            result.Add(MapToDto(attr, linkedCount));
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Retrieves a specific shared attribute by ID.
    /// </summary>
    public async Task<SharedAttributeDto?> HandleAsync(GetSharedAttributeByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sharedAttribute = await _sharedAttributeRepository.GetByIdAsync(query.SharedAttributeId, cancellationToken);
        if (sharedAttribute is null)
        {
            return null;
        }

        var linkedCount = await _sharedAttributeRepository.GetLinkedCategoryCountAsync(query.SharedAttributeId, cancellationToken);
        return MapToDto(sharedAttribute, linkedCount);
    }

    private async Task<IReadOnlyCollection<CategoryAttributeDto>> MapToDtosAsync(
        IReadOnlyCollection<CategoryAttribute> attributes,
        CancellationToken cancellationToken)
    {
        if (attributes.Count == 0)
        {
            return Array.Empty<CategoryAttributeDto>();
        }

        // Get shared attribute names
        var sharedAttributeIds = attributes
            .Where(a => a.SharedAttributeId.HasValue)
            .Select(a => a.SharedAttributeId!.Value)
            .Distinct()
            .ToList();

        var sharedAttributes = new Dictionary<Guid, string>();
        foreach (var sharedId in sharedAttributeIds)
        {
            var shared = await _sharedAttributeRepository.GetByIdAsync(sharedId, cancellationToken);
            if (shared is not null)
            {
                sharedAttributes[sharedId] = shared.Name;
            }
        }

        return attributes
            .Select(a => MapToDto(a, a.SharedAttributeId.HasValue 
                ? sharedAttributes.GetValueOrDefault(a.SharedAttributeId.Value) 
                : null))
            .ToList()
            .AsReadOnly();
    }

    private static CategoryAttributeDto MapToDto(CategoryAttribute attribute, string? sharedAttributeName)
    {
        return new CategoryAttributeDto(
            attribute.Id,
            attribute.CategoryId,
            attribute.Name,
            attribute.Type,
            GetTypeDisplay(attribute.Type),
            attribute.IsRequired,
            attribute.ListValues,
            attribute.DisplayOrder,
            attribute.IsDeprecated,
            attribute.SharedAttributeId,
            sharedAttributeName,
            attribute.CreatedAt,
            attribute.UpdatedAt);
    }

    private static SharedAttributeDto MapToDto(SharedAttribute attribute, int linkedCategoryCount)
    {
        return new SharedAttributeDto(
            attribute.Id,
            attribute.Name,
            attribute.Type,
            GetTypeDisplay(attribute.Type),
            attribute.ListValues,
            attribute.Description,
            linkedCategoryCount,
            attribute.CreatedAt,
            attribute.UpdatedAt);
    }

    private static string GetTypeDisplay(AttributeType type)
    {
        return type switch
        {
            AttributeType.Text => "Text",
            AttributeType.Number => "Number",
            AttributeType.List => "List",
            AttributeType.Boolean => "Yes/No",
            AttributeType.Date => "Date",
            _ => type.ToString()
        };
    }

    private static IReadOnlyList<string> ValidateAttribute(string name, AttributeType type, string? listValues)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Attribute name is required.");
        }
        else if (name.Trim().Length < 2)
        {
            errors.Add("Attribute name must be at least 2 characters long.");
        }
        else if (name.Trim().Length > 100)
        {
            errors.Add("Attribute name cannot exceed 100 characters.");
        }

        if (type == AttributeType.List)
        {
            if (string.IsNullOrWhiteSpace(listValues))
            {
                errors.Add("List values are required for list-type attributes.");
            }
            else if (listValues.Trim().Length > 1000)
            {
                errors.Add("List values cannot exceed 1000 characters.");
            }
        }

        return errors;
    }
}
