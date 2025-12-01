namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all currencies.
/// </summary>
public record GetAllCurrenciesQuery;

/// <summary>
/// Query to get all enabled currencies.
/// </summary>
public record GetEnabledCurrenciesQuery;

/// <summary>
/// Query to get a currency by ID.
/// </summary>
public record GetCurrencyByIdQuery(Guid CurrencyId);

/// <summary>
/// Query to get a currency by its ISO code.
/// </summary>
public record GetCurrencyByCodeQuery(string Code);

/// <summary>
/// Query to get the current base currency.
/// </summary>
public record GetBaseCurrencyQuery;
