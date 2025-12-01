using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new integration.
/// </summary>
public sealed record CreateIntegrationCommand(
    Guid AdminUserId,
    string Name,
    IntegrationType Type,
    IntegrationEnvironment Environment,
    string? Endpoint,
    string? ApiKey,
    string? MerchantId,
    string? CallbackUrl,
    string? Description);

/// <summary>
/// Command to update an existing integration.
/// </summary>
public sealed record UpdateIntegrationCommand(
    Guid IntegrationId,
    Guid AdminUserId,
    string Name,
    IntegrationType Type,
    IntegrationEnvironment Environment,
    string? Endpoint,
    string? ApiKey,
    string? MerchantId,
    string? CallbackUrl,
    string? Description);

/// <summary>
/// Command to toggle integration status (enable/disable).
/// </summary>
public sealed record ToggleIntegrationStatusCommand(
    Guid IntegrationId,
    Guid AdminUserId,
    bool Enable);

/// <summary>
/// Command to test integration connection.
/// </summary>
public sealed record TestIntegrationConnectionCommand(
    Guid IntegrationId,
    Guid AdminUserId);

/// <summary>
/// Command to delete an integration.
/// </summary>
public sealed record DeleteIntegrationCommand(
    Guid IntegrationId,
    Guid AdminUserId);
