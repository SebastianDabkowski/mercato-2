using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for integration list view.
/// </summary>
public sealed record IntegrationDto(
    Guid Id,
    string Name,
    IntegrationType Type,
    IntegrationStatus Status,
    IntegrationEnvironment Environment,
    string? Endpoint,
    string? MerchantId,
    string? CallbackUrl,
    string? Description,
    string MaskedApiKey,
    bool HasApiKey,
    DateTime? LastHealthCheckAt,
    string? LastHealthCheckMessage,
    bool? LastHealthCheckSuccess,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Result DTO for creating an integration.
/// </summary>
public sealed class CreateIntegrationResultDto
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IntegrationDto? Integration { get; init; }

    public static CreateIntegrationResultDto Succeeded(IntegrationDto integration)
        => new() { IsSuccess = true, Integration = integration };

    public static CreateIntegrationResultDto Failed(string error)
        => new() { IsSuccess = false, Errors = new[] { error } };

    public static CreateIntegrationResultDto Failed(IReadOnlyList<string> errors)
        => new() { IsSuccess = false, Errors = errors };
}

/// <summary>
/// Result DTO for updating an integration.
/// </summary>
public sealed class UpdateIntegrationResultDto
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IntegrationDto? Integration { get; init; }

    public static UpdateIntegrationResultDto Succeeded(IntegrationDto integration)
        => new() { IsSuccess = true, Integration = integration };

    public static UpdateIntegrationResultDto Failed(string error)
        => new() { IsSuccess = false, Errors = new[] { error } };

    public static UpdateIntegrationResultDto Failed(IReadOnlyList<string> errors)
        => new() { IsSuccess = false, Errors = errors };
}

/// <summary>
/// Result DTO for toggling integration status.
/// </summary>
public sealed class ToggleIntegrationStatusResultDto
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IntegrationDto? Integration { get; init; }

    public static ToggleIntegrationStatusResultDto Succeeded(IntegrationDto integration)
        => new() { IsSuccess = true, Integration = integration };

    public static ToggleIntegrationStatusResultDto Failed(string error)
        => new() { IsSuccess = false, Errors = new[] { error } };

    public static ToggleIntegrationStatusResultDto Failed(IReadOnlyList<string> errors)
        => new() { IsSuccess = false, Errors = errors };
}

/// <summary>
/// Result DTO for testing integration connection.
/// </summary>
public sealed class TestIntegrationConnectionResultDto
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime TestedAt { get; init; }
    public int? ResponseTimeMs { get; init; }

    public static TestIntegrationConnectionResultDto Success(string message, int? responseTimeMs = null)
        => new() { IsSuccess = true, Message = message, TestedAt = DateTime.UtcNow, ResponseTimeMs = responseTimeMs };

    public static TestIntegrationConnectionResultDto Failure(string message)
        => new() { IsSuccess = false, Message = message, TestedAt = DateTime.UtcNow };
}

/// <summary>
/// Result DTO for deleting an integration.
/// </summary>
public sealed class DeleteIntegrationResultDto
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static DeleteIntegrationResultDto Succeeded()
        => new() { IsSuccess = true };

    public static DeleteIntegrationResultDto Failed(string error)
        => new() { IsSuccess = false, Errors = new[] { error } };
}
