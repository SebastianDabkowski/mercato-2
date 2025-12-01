using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating integration management use cases.
/// </summary>
public sealed class IntegrationService
{
    private readonly IIntegrationRepository _repository;
    private readonly IIntegrationHealthChecker _healthChecker;
    private readonly IDataEncryptionService _encryptionService;

    public IntegrationService(
        IIntegrationRepository repository,
        IIntegrationHealthChecker healthChecker,
        IDataEncryptionService encryptionService)
    {
        _repository = repository;
        _healthChecker = healthChecker;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Creates a new integration configuration.
    /// </summary>
    public async Task<CreateIntegrationResultDto> HandleAsync(CreateIntegrationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateIntegration(command.Name, command.Endpoint);
        if (validationErrors.Count > 0)
        {
            return CreateIntegrationResultDto.Failed(validationErrors);
        }

        try
        {
            var integration = new Integration(
                Guid.NewGuid(),
                command.Name,
                command.Type,
                command.Environment,
                command.AdminUserId);

            integration.UpdateEndpoint(command.Endpoint);
            integration.UpdateMerchantId(command.MerchantId);
            integration.UpdateCallbackUrl(command.CallbackUrl);
            integration.UpdateDescription(command.Description);

            // Encrypt and store the API key if provided
            if (!string.IsNullOrWhiteSpace(command.ApiKey))
            {
                var encryptedKey = _encryptionService.Encrypt(command.ApiKey);
                integration.UpdateApiKey(encryptedKey);
            }

            await _repository.AddAsync(integration, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return CreateIntegrationResultDto.Succeeded(MapToDto(integration));
        }
        catch (ArgumentException ex)
        {
            return CreateIntegrationResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing integration.
    /// </summary>
    public async Task<UpdateIntegrationResultDto> HandleAsync(UpdateIntegrationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var integration = await _repository.GetByIdAsync(command.IntegrationId, cancellationToken);
        if (integration is null)
        {
            return UpdateIntegrationResultDto.Failed("Integration not found.");
        }

        var validationErrors = ValidateIntegration(command.Name, command.Endpoint);
        if (validationErrors.Count > 0)
        {
            return UpdateIntegrationResultDto.Failed(validationErrors);
        }

        try
        {
            integration.UpdateName(command.Name);
            integration.UpdateType(command.Type);
            integration.UpdateEnvironment(command.Environment);
            integration.UpdateEndpoint(command.Endpoint);
            integration.UpdateMerchantId(command.MerchantId);
            integration.UpdateCallbackUrl(command.CallbackUrl);
            integration.UpdateDescription(command.Description);
            integration.SetLastModifiedBy(command.AdminUserId);

            // Only update API key if a new one is provided
            if (!string.IsNullOrWhiteSpace(command.ApiKey))
            {
                var encryptedKey = _encryptionService.Encrypt(command.ApiKey);
                integration.UpdateApiKey(encryptedKey);
            }

            _repository.Update(integration);
            await _repository.SaveChangesAsync(cancellationToken);

            return UpdateIntegrationResultDto.Succeeded(MapToDto(integration));
        }
        catch (ArgumentException ex)
        {
            return UpdateIntegrationResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Toggles integration status (enable/disable).
    /// </summary>
    public async Task<ToggleIntegrationStatusResultDto> HandleAsync(ToggleIntegrationStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var integration = await _repository.GetByIdAsync(command.IntegrationId, cancellationToken);
        if (integration is null)
        {
            return ToggleIntegrationStatusResultDto.Failed("Integration not found.");
        }

        try
        {
            if (command.Enable)
            {
                var errors = integration.Enable();
                if (errors.Count > 0)
                {
                    return ToggleIntegrationStatusResultDto.Failed(errors);
                }
            }
            else
            {
                integration.Disable();
            }

            integration.SetLastModifiedBy(command.AdminUserId);
            _repository.Update(integration);
            await _repository.SaveChangesAsync(cancellationToken);

            return ToggleIntegrationStatusResultDto.Succeeded(MapToDto(integration));
        }
        catch (ArgumentException ex)
        {
            return ToggleIntegrationStatusResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Tests the connection to an integration.
    /// </summary>
    public async Task<TestIntegrationConnectionResultDto> HandleAsync(TestIntegrationConnectionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var integration = await _repository.GetByIdAsync(command.IntegrationId, cancellationToken);
        if (integration is null)
        {
            return TestIntegrationConnectionResultDto.Failure("Integration not found.");
        }

        if (string.IsNullOrWhiteSpace(integration.Endpoint))
        {
            return TestIntegrationConnectionResultDto.Failure("No endpoint configured for this integration.");
        }

        // Decrypt the API key if present
        string? decryptedApiKey = null;
        if (!string.IsNullOrWhiteSpace(integration.ApiKeyEncrypted))
        {
            decryptedApiKey = _encryptionService.Decrypt(integration.ApiKeyEncrypted);
        }

        var (success, message, responseTimeMs) = await _healthChecker.TestConnectionAsync(
            integration.Type,
            integration.Endpoint,
            decryptedApiKey,
            integration.MerchantId,
            cancellationToken);

        // Update integration health status
        if (success)
        {
            integration.RecordHealthCheckSuccess(message);
        }
        else
        {
            integration.RecordHealthCheckFailure(message);
        }

        integration.SetLastModifiedBy(command.AdminUserId);
        _repository.Update(integration);
        await _repository.SaveChangesAsync(cancellationToken);

        return success
            ? TestIntegrationConnectionResultDto.Success(message, responseTimeMs)
            : TestIntegrationConnectionResultDto.Failure(message);
    }

    /// <summary>
    /// Deletes an integration.
    /// </summary>
    public async Task<DeleteIntegrationResultDto> HandleAsync(DeleteIntegrationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var integration = await _repository.GetByIdAsync(command.IntegrationId, cancellationToken);
        if (integration is null)
        {
            return DeleteIntegrationResultDto.Failed("Integration not found.");
        }

        _repository.Delete(integration);
        await _repository.SaveChangesAsync(cancellationToken);

        return DeleteIntegrationResultDto.Succeeded();
    }

    /// <summary>
    /// Gets integrations with filtering and pagination.
    /// </summary>
    public async Task<PagedResultDto<IntegrationDto>> HandleAsync(GetIntegrationsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (integrations, totalCount) = await _repository.GetPagedAsync(
            query.Type,
            query.Status,
            query.Environment,
            query.SearchTerm,
            pageNumber,
            pageSize,
            cancellationToken);

        var items = integrations.Select(MapToDto).ToArray();
        return PagedResultDto<IntegrationDto>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Gets a single integration by ID.
    /// </summary>
    public async Task<IntegrationDto?> HandleAsync(GetIntegrationByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var integration = await _repository.GetByIdAsync(query.IntegrationId, cancellationToken);
        return integration is null ? null : MapToDto(integration);
    }

    private static IntegrationDto MapToDto(Integration integration)
    {
        return new IntegrationDto(
            integration.Id,
            integration.Name,
            integration.Type,
            integration.Status,
            integration.Environment,
            integration.Endpoint,
            integration.MerchantId,
            integration.CallbackUrl,
            integration.Description,
            integration.GetMaskedApiKey(),
            !string.IsNullOrEmpty(integration.ApiKeyEncrypted),
            integration.LastHealthCheckAt,
            integration.LastHealthCheckMessage,
            integration.LastHealthCheckSuccess,
            integration.CreatedAt,
            integration.UpdatedAt);
    }

    private static IReadOnlyList<string> ValidateIntegration(string name, string? endpoint)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Integration name is required.");
        }
        else if (name.Trim().Length < 2)
        {
            errors.Add("Integration name must be at least 2 characters long.");
        }
        else if (name.Trim().Length > 100)
        {
            errors.Add("Integration name cannot exceed 100 characters.");
        }

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add("Endpoint must be a valid HTTP or HTTPS URL.");
            }
        }

        return errors;
    }
}
