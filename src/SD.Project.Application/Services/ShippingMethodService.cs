using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating shipping method management use cases.
/// </summary>
public sealed class ShippingMethodService
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IStoreRepository _storeRepository;

    public ShippingMethodService(
        IShippingMethodRepository shippingMethodRepository,
        IStoreRepository storeRepository)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// Gets all shipping methods for a store (including inactive) for settings management.
    /// </summary>
    public async Task<IReadOnlyList<ShippingMethodSettingsDto>> HandleAsync(
        GetShippingMethodsByStoreIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var methods = await _shippingMethodRepository.GetAllByStoreIdAsync(query.StoreId, cancellationToken);
        return methods.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets a shipping method by ID.
    /// </summary>
    public async Task<ShippingMethodSettingsDto?> HandleAsync(
        GetShippingMethodByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var method = await _shippingMethodRepository.GetByIdAsync(query.ShippingMethodId, cancellationToken);
        return method is null ? null : MapToDto(method);
    }

    /// <summary>
    /// Creates a new shipping method for a store.
    /// </summary>
    public async Task<ShippingMethodSettingsResultDto> HandleAsync(
        CreateShippingMethodCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Verify store exists
        var store = await _storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
        {
            return ShippingMethodSettingsResultDto.Failed("Store not found.");
        }

        // Validate inputs
        var validationErrors = ValidateShippingMethod(command.Name, command.BaseCost, command.CostPerItem, 
            command.EstimatedDeliveryDaysMin, command.EstimatedDeliveryDaysMax);
        if (validationErrors.Count > 0)
        {
            return ShippingMethodSettingsResultDto.Failed(validationErrors);
        }

        try
        {
            // If this will be the default, remove default status from other methods
            if (command.IsDefault)
            {
                await ClearDefaultMethodAsync(command.StoreId, cancellationToken);
            }

            var shippingMethod = new ShippingMethod(
                command.StoreId,
                command.Name,
                command.Description,
                command.CarrierName,
                command.EstimatedDeliveryDaysMin,
                command.EstimatedDeliveryDaysMax,
                command.BaseCost,
                command.CostPerItem,
                command.Currency,
                command.FreeShippingThreshold,
                command.DisplayOrder,
                command.IsDefault,
                null,
                command.AvailableRegions);

            await _shippingMethodRepository.AddAsync(shippingMethod, cancellationToken);
            await _shippingMethodRepository.SaveChangesAsync(cancellationToken);

            return ShippingMethodSettingsResultDto.Succeeded(MapToDto(shippingMethod), "Shipping method created successfully.");
        }
        catch (ArgumentException ex)
        {
            return ShippingMethodSettingsResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing shipping method.
    /// </summary>
    public async Task<ShippingMethodSettingsResultDto> HandleAsync(
        UpdateShippingMethodCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shippingMethod = await _shippingMethodRepository.GetByIdAsync(command.ShippingMethodId, cancellationToken);
        if (shippingMethod is null)
        {
            return ShippingMethodSettingsResultDto.Failed("Shipping method not found.");
        }

        // Verify ownership
        if (shippingMethod.StoreId != command.StoreId)
        {
            return ShippingMethodSettingsResultDto.Failed("Shipping method does not belong to this store.");
        }

        // Validate inputs
        var validationErrors = ValidateShippingMethod(command.Name, command.BaseCost, command.CostPerItem,
            command.EstimatedDeliveryDaysMin, command.EstimatedDeliveryDaysMax);
        if (validationErrors.Count > 0)
        {
            return ShippingMethodSettingsResultDto.Failed(validationErrors);
        }

        try
        {
            shippingMethod.Update(
                command.Name,
                command.Description,
                command.CarrierName,
                command.EstimatedDeliveryDaysMin,
                command.EstimatedDeliveryDaysMax,
                command.DisplayOrder,
                command.AvailableRegions);

            shippingMethod.UpdateCosts(command.BaseCost, command.CostPerItem, command.FreeShippingThreshold);

            await _shippingMethodRepository.UpdateAsync(shippingMethod, cancellationToken);
            await _shippingMethodRepository.SaveChangesAsync(cancellationToken);

            return ShippingMethodSettingsResultDto.Succeeded(MapToDto(shippingMethod), "Shipping method updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return ShippingMethodSettingsResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Toggles the active status of a shipping method.
    /// </summary>
    public async Task<ShippingMethodSettingsResultDto> HandleAsync(
        ToggleShippingMethodStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shippingMethod = await _shippingMethodRepository.GetByIdAsync(command.ShippingMethodId, cancellationToken);
        if (shippingMethod is null)
        {
            return ShippingMethodSettingsResultDto.Failed("Shipping method not found.");
        }

        // Verify ownership
        if (shippingMethod.StoreId != command.StoreId)
        {
            return ShippingMethodSettingsResultDto.Failed("Shipping method does not belong to this store.");
        }

        if (command.IsActive)
        {
            shippingMethod.Activate();
        }
        else
        {
            shippingMethod.Deactivate();
        }

        await _shippingMethodRepository.UpdateAsync(shippingMethod, cancellationToken);
        await _shippingMethodRepository.SaveChangesAsync(cancellationToken);

        var message = command.IsActive
            ? "Shipping method activated successfully."
            : "Shipping method deactivated successfully.";

        return ShippingMethodSettingsResultDto.Succeeded(MapToDto(shippingMethod), message);
    }

    /// <summary>
    /// Deletes a shipping method.
    /// </summary>
    public async Task<ShippingMethodSettingsResultDto> HandleAsync(
        DeleteShippingMethodCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shippingMethod = await _shippingMethodRepository.GetByIdAsync(command.ShippingMethodId, cancellationToken);
        if (shippingMethod is null)
        {
            return ShippingMethodSettingsResultDto.Failed("Shipping method not found.");
        }

        // Verify ownership
        if (shippingMethod.StoreId != command.StoreId)
        {
            return ShippingMethodSettingsResultDto.Failed("Shipping method does not belong to this store.");
        }

        await _shippingMethodRepository.DeleteAsync(shippingMethod, cancellationToken);
        await _shippingMethodRepository.SaveChangesAsync(cancellationToken);

        return ShippingMethodSettingsResultDto.Succeeded("Shipping method deleted successfully.");
    }

    private async Task ClearDefaultMethodAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var currentDefault = await _shippingMethodRepository.GetDefaultByStoreIdAsync(storeId, cancellationToken);
        if (currentDefault is not null)
        {
            currentDefault.RemoveDefault();
            await _shippingMethodRepository.UpdateAsync(currentDefault, cancellationToken);
        }
    }

    private static IReadOnlyList<string> ValidateShippingMethod(
        string name,
        decimal baseCost,
        decimal costPerItem,
        int minDays,
        int maxDays)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Shipping method name is required.");
        }
        else if (name.Trim().Length < 2)
        {
            errors.Add("Shipping method name must be at least 2 characters long.");
        }
        else if (name.Trim().Length > 100)
        {
            errors.Add("Shipping method name cannot exceed 100 characters.");
        }

        if (baseCost < 0)
        {
            errors.Add("Base cost cannot be negative.");
        }

        if (costPerItem < 0)
        {
            errors.Add("Cost per item cannot be negative.");
        }

        if (minDays < 0)
        {
            errors.Add("Minimum delivery days cannot be negative.");
        }

        if (maxDays < minDays)
        {
            errors.Add("Maximum delivery days cannot be less than minimum.");
        }

        return errors;
    }

    private static ShippingMethodSettingsDto MapToDto(ShippingMethod method)
    {
        return new ShippingMethodSettingsDto(
            method.Id,
            method.StoreId,
            method.Name,
            method.Description,
            method.CarrierName,
            method.EstimatedDeliveryDaysMin,
            method.EstimatedDeliveryDaysMax,
            method.BaseCost,
            method.CostPerItem,
            method.FreeShippingThreshold,
            method.Currency,
            method.DisplayOrder,
            method.AvailableRegions,
            method.IsDefault,
            method.IsActive,
            method.CreatedAt,
            method.UpdatedAt);
    }
}
