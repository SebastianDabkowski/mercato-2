namespace SD.Project.ViewModels;

/// <summary>
/// View model for a currency row in the list view.
/// </summary>
public sealed class CurrencyViewModel
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Symbol { get; init; } = default!;
    public int DecimalPlaces { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsBaseCurrency { get; init; }
    public decimal ExchangeRate { get; init; }
    public DateTime? ExchangeRateUpdatedAt { get; init; }
    public string? ExchangeRateSource { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets a formatted display string showing code and name.
    /// </summary>
    public string DisplayName => $"{Code} - {Name}";

    /// <summary>
    /// Gets a formatted display string showing symbol and code.
    /// </summary>
    public string SymbolDisplay => $"{Symbol} ({Code})";

    /// <summary>
    /// Gets the formatted exchange rate.
    /// </summary>
    public string ExchangeRateDisplay => IsBaseCurrency
        ? "1.0 (Base)"
        : $"{ExchangeRate:F6}";

    /// <summary>
    /// Gets exchange rate information with source and update time.
    /// </summary>
    public string ExchangeRateInfo
    {
        get
        {
            if (IsBaseCurrency)
            {
                return "Base currency (1.0)";
            }

            var parts = new List<string> { $"Rate: {ExchangeRate:F6}" };

            if (ExchangeRateSource is not null)
            {
                parts.Add($"Source: {ExchangeRateSource}");
            }

            if (ExchangeRateUpdatedAt.HasValue)
            {
                parts.Add($"Updated: {ExchangeRateUpdatedAt:MMM d, yyyy 'at' h:mm tt}");
            }
            else
            {
                parts.Add("Never updated");
            }

            return string.Join(" • ", parts);
        }
    }

    /// <summary>
    /// Gets the status badge CSS class.
    /// </summary>
    public string StatusBadgeClass
    {
        get
        {
            if (IsBaseCurrency) return "bg-primary";
            if (IsEnabled) return "bg-success";
            return "bg-secondary";
        }
    }

    /// <summary>
    /// Gets the status display text.
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            if (IsBaseCurrency) return "Base Currency";
            if (IsEnabled) return "Enabled";
            return "Disabled";
        }
    }

    /// <summary>
    /// Gets the last updated info for display.
    /// </summary>
    public string LastUpdatedDisplay => $"{UpdatedAt:MMM d, yyyy 'at' h:mm tt}";

    /// <summary>
    /// Gets whether the exchange rate is stale (not updated in 24 hours).
    /// </summary>
    public bool IsExchangeRateStale => !IsBaseCurrency &&
        (!ExchangeRateUpdatedAt.HasValue ||
         ExchangeRateUpdatedAt.Value < DateTime.UtcNow.AddHours(-24));
}
