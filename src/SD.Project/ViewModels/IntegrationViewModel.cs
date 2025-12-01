using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying an integration in the list view.
/// </summary>
public sealed class IntegrationViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public IntegrationType Type { get; init; }
    public IntegrationStatus Status { get; init; }
    public IntegrationEnvironment Environment { get; init; }
    public string? Endpoint { get; init; }
    public string? MerchantId { get; init; }
    public string? CallbackUrl { get; init; }
    public string? Description { get; init; }
    public string MaskedApiKey { get; init; } = string.Empty;
    public bool HasApiKey { get; init; }
    public DateTime? LastHealthCheckAt { get; init; }
    public string? LastHealthCheckMessage { get; init; }
    public bool? LastHealthCheckSuccess { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the display name for the integration type.
    /// </summary>
    public string TypeDisplayName => Type switch
    {
        IntegrationType.Payment => "Payment Gateway",
        IntegrationType.Shipping => "Shipping Provider",
        IntegrationType.Erp => "ERP System",
        IntegrationType.Ecommerce => "E-commerce Connector",
        IntegrationType.Other => "Other",
        _ => Type.ToString()
    };

    /// <summary>
    /// Gets the display name for the status.
    /// </summary>
    public string StatusDisplayName => Status switch
    {
        IntegrationStatus.Active => "Active",
        IntegrationStatus.Disabled => "Disabled",
        IntegrationStatus.Unhealthy => "Unhealthy",
        IntegrationStatus.Pending => "Pending Setup",
        _ => Status.ToString()
    };

    /// <summary>
    /// Gets the CSS class for the status badge.
    /// </summary>
    public string StatusCssClass => Status switch
    {
        IntegrationStatus.Active => "badge bg-success",
        IntegrationStatus.Disabled => "badge bg-secondary",
        IntegrationStatus.Unhealthy => "badge bg-danger",
        IntegrationStatus.Pending => "badge bg-warning text-dark",
        _ => "badge bg-secondary"
    };

    /// <summary>
    /// Gets the display name for the environment.
    /// </summary>
    public string EnvironmentDisplayName => Environment switch
    {
        IntegrationEnvironment.Sandbox => "Sandbox",
        IntegrationEnvironment.Production => "Production",
        _ => Environment.ToString()
    };

    /// <summary>
    /// Gets the CSS class for the environment badge.
    /// </summary>
    public string EnvironmentCssClass => Environment switch
    {
        IntegrationEnvironment.Sandbox => "badge bg-info text-dark",
        IntegrationEnvironment.Production => "badge bg-primary",
        _ => "badge bg-secondary"
    };

    /// <summary>
    /// Gets the CSS class for the health check status icon.
    /// </summary>
    public string HealthCheckIconClass => LastHealthCheckSuccess switch
    {
        true => "text-success",
        false => "text-danger",
        null => "text-muted"
    };

    /// <summary>
    /// Checks if the integration can be enabled.
    /// </summary>
    public bool CanBeEnabled => Status != IntegrationStatus.Active && !string.IsNullOrEmpty(Endpoint);

    /// <summary>
    /// Checks if the integration can be disabled.
    /// </summary>
    public bool CanBeDisabled => Status == IntegrationStatus.Active || Status == IntegrationStatus.Unhealthy;
}
