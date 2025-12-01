namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the type/name of a shipping provider.
/// </summary>
public enum ShippingProviderType
{
    /// <summary>Manual shipping (no provider integration).</summary>
    Manual,
    /// <summary>DHL shipping provider.</summary>
    Dhl,
    /// <summary>UPS shipping provider.</summary>
    Ups,
    /// <summary>FedEx shipping provider.</summary>
    FedEx,
    /// <summary>InPost shipping provider (Poland).</summary>
    InPost
}

/// <summary>
/// Represents a shipping provider integration configured for a store or platform.
/// Sellers can enable configured providers for their shipping settings.
/// </summary>
public class ShippingProvider
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The store this provider is configured for.
    /// Null for platform-wide provider configurations.
    /// </summary>
    public Guid? StoreId { get; private set; }

    /// <summary>
    /// The type of shipping provider.
    /// </summary>
    public ShippingProviderType ProviderType { get; private set; }

    /// <summary>
    /// Display name for this provider configuration.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// API key or client ID for authentication.
    /// Stored encrypted in production.
    /// </summary>
    public string? ApiKey { get; private set; }

    /// <summary>
    /// API secret or client secret for authentication.
    /// Stored encrypted in production.
    /// </summary>
    public string? ApiSecret { get; private set; }

    /// <summary>
    /// Account number with the provider (e.g., DHL account number).
    /// </summary>
    public string? AccountNumber { get; private set; }

    /// <summary>
    /// Base URL for the provider API.
    /// Null uses the default for the provider type.
    /// </summary>
    public string? ApiBaseUrl { get; private set; }

    /// <summary>
    /// Whether this provider is enabled and can be used.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Whether to use the sandbox/test environment.
    /// </summary>
    public bool UseSandbox { get; private set; }

    /// <summary>
    /// Maximum number of retry attempts for failed API calls.
    /// </summary>
    public int MaxRetryAttempts { get; private set; }

    /// <summary>
    /// Retry delay in seconds between attempts.
    /// </summary>
    public int RetryDelaySeconds { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ShippingProvider()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new shipping provider configuration.
    /// </summary>
    public ShippingProvider(
        Guid? storeId,
        ShippingProviderType providerType,
        string name,
        string? apiKey = null,
        string? apiSecret = null,
        string? accountNumber = null,
        string? apiBaseUrl = null,
        bool useSandbox = true,
        int maxRetryAttempts = 3,
        int retryDelaySeconds = 5)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name is required.", nameof(name));
        }

        if (maxRetryAttempts < 0)
        {
            throw new ArgumentException("Max retry attempts cannot be negative.", nameof(maxRetryAttempts));
        }

        if (retryDelaySeconds < 0)
        {
            throw new ArgumentException("Retry delay cannot be negative.", nameof(retryDelaySeconds));
        }

        Id = Guid.NewGuid();
        StoreId = storeId;
        ProviderType = providerType;
        Name = name.Trim();
        ApiKey = apiKey?.Trim();
        ApiSecret = apiSecret?.Trim();
        AccountNumber = accountNumber?.Trim();
        ApiBaseUrl = apiBaseUrl?.Trim();
        IsEnabled = true;
        UseSandbox = useSandbox;
        MaxRetryAttempts = maxRetryAttempts;
        RetryDelaySeconds = retryDelaySeconds;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the provider credentials.
    /// </summary>
    public void UpdateCredentials(string? apiKey, string? apiSecret, string? accountNumber)
    {
        ApiKey = apiKey?.Trim();
        ApiSecret = apiSecret?.Trim();
        AccountNumber = accountNumber?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the provider settings.
    /// </summary>
    public void UpdateSettings(
        string name,
        string? apiBaseUrl,
        bool useSandbox,
        int maxRetryAttempts,
        int retryDelaySeconds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name is required.", nameof(name));
        }

        Name = name.Trim();
        ApiBaseUrl = apiBaseUrl?.Trim();
        UseSandbox = useSandbox;
        MaxRetryAttempts = maxRetryAttempts;
        RetryDelaySeconds = retryDelaySeconds;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables the provider.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the provider.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the provider has valid credentials configured.
    /// </summary>
    public bool HasValidCredentials()
    {
        // Manual providers don't need credentials
        if (ProviderType == ShippingProviderType.Manual)
        {
            return true;
        }

        // Other providers need at least an API key
        return !string.IsNullOrWhiteSpace(ApiKey);
    }
}
