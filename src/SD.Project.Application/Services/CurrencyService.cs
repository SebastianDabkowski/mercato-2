using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing currencies and platform currency settings.
/// </summary>
public sealed class CurrencyService
{
    private readonly ICurrencyRepository _repository;

    public CurrencyService(ICurrencyRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>
    /// Gets all currencies.
    /// </summary>
    public async Task<IReadOnlyCollection<CurrencyDto>> HandleAsync(
        GetAllCurrenciesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currencies = await _repository.GetAllAsync(cancellationToken);
        return currencies.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets all enabled currencies.
    /// </summary>
    public async Task<IReadOnlyCollection<CurrencyDto>> HandleAsync(
        GetEnabledCurrenciesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currencies = await _repository.GetEnabledAsync(cancellationToken);
        return currencies.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets a currency by ID.
    /// </summary>
    public async Task<CurrencyDto?> HandleAsync(
        GetCurrencyByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currency = await _repository.GetByIdAsync(query.CurrencyId, cancellationToken);
        return currency is null ? null : MapToDto(currency);
    }

    /// <summary>
    /// Gets a currency by its ISO code.
    /// </summary>
    public async Task<CurrencyDto?> HandleAsync(
        GetCurrencyByCodeQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currency = await _repository.GetByCodeAsync(query.Code, cancellationToken);
        return currency is null ? null : MapToDto(currency);
    }

    /// <summary>
    /// Gets the current base currency.
    /// </summary>
    public async Task<CurrencyDto?> HandleAsync(
        GetBaseCurrencyQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currency = await _repository.GetBaseCurrencyAsync(cancellationToken);
        return currency is null ? null : MapToDto(currency);
    }

    /// <summary>
    /// Creates a new currency.
    /// </summary>
    public async Task<CurrencyResultDto> HandleAsync(
        CreateCurrencyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate inputs
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            validationErrors.Add("Currency code is required.");
        }
        else if (command.Code.Length != 3)
        {
            validationErrors.Add("Currency code must be a 3-letter ISO 4217 code.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            validationErrors.Add("Currency name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Symbol))
        {
            validationErrors.Add("Currency symbol is required.");
        }

        if (command.DecimalPlaces < 0 || command.DecimalPlaces > 8)
        {
            validationErrors.Add("Decimal places must be between 0 and 8.");
        }

        if (validationErrors.Count > 0)
        {
            return CurrencyResultDto.Failed(validationErrors);
        }

        // Check if currency code already exists
        var exists = await _repository.ExistsAsync(command.Code, cancellationToken);
        if (exists)
        {
            return CurrencyResultDto.Failed($"Currency with code '{command.Code.ToUpperInvariant()}' already exists.");
        }

        // Create the currency
        Currency currency;
        try
        {
            currency = new Currency(
                command.Code,
                command.Name,
                command.Symbol,
                command.DecimalPlaces,
                command.DisplayOrder);
        }
        catch (ArgumentException ex)
        {
            return CurrencyResultDto.Failed(ex.Message);
        }

        await _repository.AddAsync(currency, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return CurrencyResultDto.Succeeded(MapToDto(currency), "Currency created successfully.");
    }

    /// <summary>
    /// Updates an existing currency.
    /// </summary>
    public async Task<CurrencyResultDto> HandleAsync(
        UpdateCurrencyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currency = await _repository.GetByIdAsync(command.CurrencyId, cancellationToken);
        if (currency is null)
        {
            return CurrencyResultDto.Failed("Currency not found.");
        }

        // Validate inputs
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            validationErrors.Add("Currency name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Symbol))
        {
            validationErrors.Add("Currency symbol is required.");
        }

        if (command.DecimalPlaces < 0 || command.DecimalPlaces > 8)
        {
            validationErrors.Add("Decimal places must be between 0 and 8.");
        }

        if (validationErrors.Count > 0)
        {
            return CurrencyResultDto.Failed(validationErrors);
        }

        // Update the currency
        try
        {
            currency.UpdateName(command.Name);
            currency.UpdateSymbol(command.Symbol);
            currency.UpdateDecimalPlaces(command.DecimalPlaces);
            currency.UpdateDisplayOrder(command.DisplayOrder);
        }
        catch (ArgumentException ex)
        {
            return CurrencyResultDto.Failed(ex.Message);
        }

        await _repository.UpdateAsync(currency, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return CurrencyResultDto.Succeeded(MapToDto(currency), "Currency updated successfully.");
    }

    /// <summary>
    /// Enables a currency.
    /// </summary>
    public async Task<CurrencyResultDto> HandleAsync(
        EnableCurrencyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currency = await _repository.GetByIdAsync(command.CurrencyId, cancellationToken);
        if (currency is null)
        {
            return CurrencyResultDto.Failed("Currency not found.");
        }

        if (currency.IsEnabled)
        {
            return CurrencyResultDto.Failed("Currency is already enabled.");
        }

        currency.Enable();
        await _repository.UpdateAsync(currency, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return CurrencyResultDto.Succeeded(MapToDto(currency), "Currency enabled successfully.");
    }

    /// <summary>
    /// Disables a currency.
    /// </summary>
    public async Task<CurrencyResultDto> HandleAsync(
        DisableCurrencyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currency = await _repository.GetByIdAsync(command.CurrencyId, cancellationToken);
        if (currency is null)
        {
            return CurrencyResultDto.Failed("Currency not found.");
        }

        if (!currency.IsEnabled)
        {
            return CurrencyResultDto.Failed("Currency is already disabled.");
        }

        var errors = currency.Disable();
        if (errors.Count > 0)
        {
            return CurrencyResultDto.Failed(errors);
        }

        await _repository.UpdateAsync(currency, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return CurrencyResultDto.Succeeded(MapToDto(currency), "Currency disabled successfully. Historical data remains unaffected.");
    }

    /// <summary>
    /// Sets a currency as the platform's base currency.
    /// Requires confirmation for this significant operation.
    /// </summary>
    public async Task<SetBaseCurrencyResultDto> HandleAsync(
        SetBaseCurrencyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currency = await _repository.GetByIdAsync(command.CurrencyId, cancellationToken);
        if (currency is null)
        {
            return SetBaseCurrencyResultDto.Failed("Currency not found.");
        }

        if (currency.IsBaseCurrency)
        {
            return SetBaseCurrencyResultDto.Failed("This currency is already the base currency.");
        }

        // Get current base currency for comparison
        var currentBase = await _repository.GetBaseCurrencyAsync(cancellationToken);
        var currentBaseDto = currentBase is null ? null : MapToDto(currentBase);

        // Require confirmation for this significant operation
        if (!command.Confirmed)
        {
            var warning = currentBase is not null
                ? $"Changing the base currency from {currentBase.Code} to {currency.Code} will affect all pricing calculations across the platform. This is a significant change that may impact existing listings and reports. Please confirm this action."
                : $"Setting {currency.Code} as the base currency will affect all pricing calculations across the platform. Please confirm this action.";

            return SetBaseCurrencyResultDto.NeedsConfirmation(warning, currentBaseDto);
        }

        // Perform the base currency change
        var errors = currency.SetAsBaseCurrency();
        if (errors.Count > 0)
        {
            return SetBaseCurrencyResultDto.Failed(errors);
        }

        // Remove base status from the previous base currency
        if (currentBase is not null)
        {
            currentBase.RemoveBaseCurrencyStatus();
            await _repository.UpdateAsync(currentBase, cancellationToken);
        }

        await _repository.UpdateAsync(currency, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return SetBaseCurrencyResultDto.Succeeded(
            MapToDto(currency),
            currentBaseDto,
            $"Base currency changed to {currency.Code} successfully.");
    }

    /// <summary>
    /// Updates the exchange rate for a currency.
    /// </summary>
    public async Task<CurrencyResultDto> HandleAsync(
        UpdateExchangeRateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currency = await _repository.GetByIdAsync(command.CurrencyId, cancellationToken);
        if (currency is null)
        {
            return CurrencyResultDto.Failed("Currency not found.");
        }

        if (currency.IsBaseCurrency)
        {
            return CurrencyResultDto.Failed("Cannot update exchange rate for the base currency.");
        }

        if (command.Rate <= 0)
        {
            return CurrencyResultDto.Failed("Exchange rate must be a positive number.");
        }

        try
        {
            currency.UpdateExchangeRate(command.Rate, command.Source);
        }
        catch (ArgumentException ex)
        {
            return CurrencyResultDto.Failed(ex.Message);
        }

        await _repository.UpdateAsync(currency, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return CurrencyResultDto.Succeeded(MapToDto(currency), "Exchange rate updated successfully.");
    }

    private static CurrencyDto MapToDto(Currency currency)
    {
        return new CurrencyDto(
            currency.Id,
            currency.Code,
            currency.Name,
            currency.Symbol,
            currency.DecimalPlaces,
            currency.IsEnabled,
            currency.IsBaseCurrency,
            currency.ExchangeRate,
            currency.ExchangeRateUpdatedAt,
            currency.ExchangeRateSource,
            currency.DisplayOrder,
            currency.CreatedAt,
            currency.UpdatedAt);
    }
}
