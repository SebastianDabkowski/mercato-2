namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an external integration configuration for the marketplace.
/// Stores connection details for payment providers, shipping, ERP, and other systems.
/// </summary>
public class Integration
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public IntegrationType Type { get; private set; }
    public IntegrationStatus Status { get; private set; }
    public IntegrationEnvironment Environment { get; private set; }

    /// <summary>
    /// API endpoint URL for the integration.
    /// </summary>
    public string? Endpoint { get; private set; }

    /// <summary>
    /// Encrypted API key. Never expose the full value once saved.
    /// </summary>
    public string? ApiKeyEncrypted { get; private set; }

    /// <summary>
    /// Merchant ID or account identifier for the provider.
    /// </summary>
    public string? MerchantId { get; private set; }

    /// <summary>
    /// Callback URL for webhooks from the provider.
    /// </summary>
    public string? CallbackUrl { get; private set; }

    /// <summary>
    /// Optional description or notes about this integration.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Last time a health check was performed.
    /// </summary>
    public DateTime? LastHealthCheckAt { get; private set; }

    /// <summary>
    /// Last health check result message.
    /// </summary>
    public string? LastHealthCheckMessage { get; private set; }

    /// <summary>
    /// Whether the last health check was successful.
    /// </summary>
    public bool? LastHealthCheckSuccess { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// ID of the admin who created this integration.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// ID of the admin who last modified this integration.
    /// </summary>
    public Guid? LastModifiedByUserId { get; private set; }

    private Integration()
    {
        // EF Core constructor
    }

    public Integration(Guid id, string name, IntegrationType type, IntegrationEnvironment environment, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Integration name is required.", nameof(name));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name.Trim();
        Type = type;
        Status = IntegrationStatus.Pending;
        Environment = environment;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the integration name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Integration name is required.", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the integration type.
    /// </summary>
    public void UpdateType(IntegrationType type)
    {
        Type = type;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the integration environment.
    /// </summary>
    public void UpdateEnvironment(IntegrationEnvironment environment)
    {
        Environment = environment;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the API endpoint.
    /// </summary>
    public void UpdateEndpoint(string? endpoint)
    {
        Endpoint = endpoint?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the encrypted API key.
    /// </summary>
    public void UpdateApiKey(string? apiKeyEncrypted)
    {
        ApiKeyEncrypted = apiKeyEncrypted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the merchant ID.
    /// </summary>
    public void UpdateMerchantId(string? merchantId)
    {
        MerchantId = merchantId?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the callback URL.
    /// </summary>
    public void UpdateCallbackUrl(string? callbackUrl)
    {
        CallbackUrl = callbackUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the user who last modified this integration.
    /// </summary>
    public void SetLastModifiedBy(Guid userId)
    {
        LastModifiedByUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables the integration.
    /// </summary>
    /// <returns>List of validation errors. Empty if successful.</returns>
    public IReadOnlyList<string> Enable()
    {
        var errors = ValidateForActivation();
        if (errors.Count > 0)
        {
            return errors;
        }

        Status = IntegrationStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        return Array.Empty<string>();
    }

    /// <summary>
    /// Disables the integration.
    /// </summary>
    public void Disable()
    {
        Status = IntegrationStatus.Disabled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the integration as unhealthy.
    /// </summary>
    public void MarkUnhealthy(string? message = null)
    {
        Status = IntegrationStatus.Unhealthy;
        LastHealthCheckAt = DateTime.UtcNow;
        LastHealthCheckSuccess = false;
        LastHealthCheckMessage = message ?? "Health check failed.";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a successful health check.
    /// </summary>
    public void RecordHealthCheckSuccess(string? message = null)
    {
        LastHealthCheckAt = DateTime.UtcNow;
        LastHealthCheckSuccess = true;
        LastHealthCheckMessage = message ?? "Connection successful.";

        // If was unhealthy, set back to active
        if (Status == IntegrationStatus.Unhealthy)
        {
            Status = IntegrationStatus.Active;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed health check.
    /// </summary>
    public void RecordHealthCheckFailure(string message)
    {
        LastHealthCheckAt = DateTime.UtcNow;
        LastHealthCheckSuccess = false;
        LastHealthCheckMessage = message;
        Status = IntegrationStatus.Unhealthy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates that the integration has minimum required configuration for activation.
    /// </summary>
    public IReadOnlyList<string> ValidateForActivation()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            errors.Add("API endpoint is required to enable the integration.");
        }

        return errors;
    }

    /// <summary>
    /// Checks if the integration is currently operational.
    /// </summary>
    public bool IsOperational => Status == IntegrationStatus.Active;

    /// <summary>
    /// Checks if the integration is disabled.
    /// </summary>
    public bool IsDisabled => Status == IntegrationStatus.Disabled;

    /// <summary>
    /// Gets a masked version of the API key (shows only last 4 characters).
    /// </summary>
    public string GetMaskedApiKey()
    {
        if (string.IsNullOrEmpty(ApiKeyEncrypted))
        {
            return string.Empty;
        }

        // Show asterisks with last 4 characters visible
        const int visibleChars = 4;
        if (ApiKeyEncrypted.Length <= visibleChars)
        {
            return new string('*', ApiKeyEncrypted.Length);
        }

        return new string('*', 12) + ApiKeyEncrypted[^visibleChars..];
        
    }
}
