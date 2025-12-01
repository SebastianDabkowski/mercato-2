namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a currency configuration for the platform.
/// Currencies can be enabled/disabled for listings and transactions.
/// </summary>
public class Currency
{
    public Guid Id { get; private set; }

    /// <summary>
    /// ISO 4217 currency code (e.g., "USD", "EUR", "PLN").
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>
    /// Human-readable currency name (e.g., "US Dollar", "Euro").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Currency symbol (e.g., "$", "€", "zł").
    /// </summary>
    public string Symbol { get; private set; } = default!;

    /// <summary>
    /// Number of decimal places for the currency (typically 2, but can be 0 for some currencies).
    /// </summary>
    public int DecimalPlaces { get; private set; }

    /// <summary>
    /// Whether this currency is enabled for new listings and transactions.
    /// Disabling a currency doesn't affect historical data.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Whether this is the platform's base currency.
    /// Only one currency can be the base currency at a time.
    /// </summary>
    public bool IsBaseCurrency { get; private set; }

    /// <summary>
    /// The exchange rate relative to the base currency.
    /// For the base currency, this is always 1.0.
    /// </summary>
    public decimal ExchangeRate { get; private set; }

    /// <summary>
    /// When the exchange rate was last updated.
    /// Null if never updated (e.g., for base currency or newly added currency).
    /// </summary>
    public DateTime? ExchangeRateUpdatedAt { get; private set; }

    /// <summary>
    /// Source of the exchange rate (e.g., "ECB", "Fixer.io", "Manual").
    /// </summary>
    public string? ExchangeRateSource { get; private set; }

    /// <summary>
    /// Display order for sorting currencies in UI.
    /// </summary>
    public int DisplayOrder { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Currency()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new currency.
    /// </summary>
    /// <param name="code">ISO 4217 currency code.</param>
    /// <param name="name">Human-readable currency name.</param>
    /// <param name="symbol">Currency symbol.</param>
    /// <param name="decimalPlaces">Number of decimal places.</param>
    /// <param name="displayOrder">Display order for UI sorting.</param>
    public Currency(
        string code,
        string name,
        string symbol,
        int decimalPlaces = 2,
        int displayOrder = 0)
    {
        ValidateCode(code);
        ValidateName(name);
        ValidateSymbol(symbol);
        ValidateDecimalPlaces(decimalPlaces);

        Id = Guid.NewGuid();
        Code = code.ToUpperInvariant();
        Name = name.Trim();
        Symbol = symbol.Trim();
        DecimalPlaces = decimalPlaces;
        DisplayOrder = displayOrder;
        IsEnabled = true;
        IsBaseCurrency = false;
        ExchangeRate = 1.0m;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the currency name.
    /// </summary>
    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the currency symbol.
    /// </summary>
    public void UpdateSymbol(string symbol)
    {
        ValidateSymbol(symbol);
        Symbol = symbol.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the number of decimal places.
    /// </summary>
    public void UpdateDecimalPlaces(int decimalPlaces)
    {
        ValidateDecimalPlaces(decimalPlaces);
        DecimalPlaces = decimalPlaces;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the display order.
    /// </summary>
    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables the currency for new listings and transactions.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the currency for new listings and transactions.
    /// Existing historical data is not affected.
    /// </summary>
    /// <returns>List of validation errors. Empty if successful.</returns>
    public IReadOnlyList<string> Disable()
    {
        if (IsBaseCurrency)
        {
            return new[] { "Cannot disable the base currency. Set another currency as base first." };
        }

        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
        return Array.Empty<string>();
    }

    /// <summary>
    /// Sets this currency as the platform's base currency.
    /// </summary>
    /// <returns>List of validation errors. Empty if successful.</returns>
    public IReadOnlyList<string> SetAsBaseCurrency()
    {
        if (!IsEnabled)
        {
            return new[] { "Cannot set a disabled currency as the base currency. Enable it first." };
        }

        IsBaseCurrency = true;
        ExchangeRate = 1.0m;
        ExchangeRateSource = null;
        ExchangeRateUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return Array.Empty<string>();
    }

    /// <summary>
    /// Removes the base currency status from this currency.
    /// Called when another currency is set as base.
    /// </summary>
    public void RemoveBaseCurrencyStatus()
    {
        IsBaseCurrency = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the exchange rate from an external source.
    /// </summary>
    /// <param name="rate">The new exchange rate relative to base currency.</param>
    /// <param name="source">Source of the exchange rate.</param>
    public void UpdateExchangeRate(decimal rate, string? source)
    {
        if (IsBaseCurrency)
        {
            // Base currency always has rate of 1.0
            return;
        }

        if (rate <= 0)
        {
            throw new ArgumentException("Exchange rate must be positive.", nameof(rate));
        }

        ExchangeRate = rate;
        ExchangeRateSource = source?.Trim();
        ExchangeRateUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Currency code is required.", nameof(code));
        }

        if (code.Length != 3)
        {
            throw new ArgumentException("Currency code must be a 3-letter ISO 4217 code.", nameof(code));
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Currency name is required.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Currency name cannot exceed 100 characters.", nameof(name));
        }
    }

    private static void ValidateSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Currency symbol is required.", nameof(symbol));
        }

        if (symbol.Length > 10)
        {
            throw new ArgumentException("Currency symbol cannot exceed 10 characters.", nameof(symbol));
        }
    }

    private static void ValidateDecimalPlaces(int decimalPlaces)
    {
        if (decimalPlaces < 0 || decimalPlaces > 8)
        {
            throw new ArgumentException("Decimal places must be between 0 and 8.", nameof(decimalPlaces));
        }
    }
}
