namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new currency.
/// </summary>
public record CreateCurrencyCommand(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces,
    int DisplayOrder);

/// <summary>
/// Command to update an existing currency.
/// </summary>
public record UpdateCurrencyCommand(
    Guid CurrencyId,
    string Name,
    string Symbol,
    int DecimalPlaces,
    int DisplayOrder);

/// <summary>
/// Command to enable a currency for listings and transactions.
/// </summary>
public record EnableCurrencyCommand(Guid CurrencyId);

/// <summary>
/// Command to disable a currency for new listings and transactions.
/// Existing historical data is not affected.
/// </summary>
public record DisableCurrencyCommand(Guid CurrencyId);

/// <summary>
/// Command to set a currency as the platform's base currency.
/// This is a significant operation that may affect pricing across the platform.
/// </summary>
public record SetBaseCurrencyCommand(
    Guid CurrencyId,
    bool Confirmed);

/// <summary>
/// Command to update exchange rate for a currency.
/// Typically called by an external integration module.
/// </summary>
public record UpdateExchangeRateCommand(
    Guid CurrencyId,
    decimal Rate,
    string? Source);
