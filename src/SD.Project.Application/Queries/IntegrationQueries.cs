using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all integrations with optional filtering.
/// </summary>
public sealed record GetIntegrationsQuery(
    IntegrationType? Type = null,
    IntegrationStatus? Status = null,
    IntegrationEnvironment? Environment = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get a specific integration by ID.
/// </summary>
public sealed record GetIntegrationByIdQuery(Guid IntegrationId);
