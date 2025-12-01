namespace SD.Project.Application.DTOs;

/// <summary>
/// Data transfer object for currency information.
/// </summary>
public record CurrencyDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces,
    bool IsEnabled,
    bool IsBaseCurrency,
    decimal ExchangeRate,
    DateTime? ExchangeRateUpdatedAt,
    string? ExchangeRateSource,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>
    /// Gets a formatted display string for the exchange rate info.
    /// </summary>
    public string ExchangeRateInfo => IsBaseCurrency
        ? "Base currency (1.0)"
        : ExchangeRateUpdatedAt.HasValue
            ? $"{ExchangeRate:F6} (Updated: {ExchangeRateUpdatedAt:g})"
            : $"{ExchangeRate:F6} (Never updated)";

    /// <summary>
    /// Gets a formatted display string showing code and name.
    /// </summary>
    public string DisplayName => $"{Code} - {Name}";
}

/// <summary>
/// Result DTO for currency operations.
/// </summary>
public record CurrencyResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public CurrencyDto? Currency { get; init; }

    public static CurrencyResultDto Succeeded(CurrencyDto currency, string message) => new()
    {
        Success = true,
        Message = message,
        Currency = currency
    };

    public static CurrencyResultDto Failed(string error) => new()
    {
        Success = false,
        Errors = new[] { error }
    };

    public static CurrencyResultDto Failed(IEnumerable<string> errors) => new()
    {
        Success = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Result DTO for base currency change operations.
/// Includes confirmation requirement flag for impactful changes.
/// </summary>
public record SetBaseCurrencyResultDto
{
    public bool Success { get; init; }
    public bool RequiresConfirmation { get; init; }
    public string? Message { get; init; }
    public string? WarningMessage { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public CurrencyDto? Currency { get; init; }
    public CurrencyDto? PreviousBaseCurrency { get; init; }

    public static SetBaseCurrencyResultDto NeedsConfirmation(string warning, CurrencyDto? previousBase) => new()
    {
        Success = false,
        RequiresConfirmation = true,
        WarningMessage = warning,
        PreviousBaseCurrency = previousBase
    };

    public static SetBaseCurrencyResultDto Succeeded(CurrencyDto currency, CurrencyDto? previousBase, string message) => new()
    {
        Success = true,
        Message = message,
        Currency = currency,
        PreviousBaseCurrency = previousBase
    };

    public static SetBaseCurrencyResultDto Failed(string error) => new()
    {
        Success = false,
        Errors = new[] { error }
    };

    public static SetBaseCurrencyResultDto Failed(IEnumerable<string> errors) => new()
    {
        Success = false,
        Errors = errors.ToList()
    };
}
