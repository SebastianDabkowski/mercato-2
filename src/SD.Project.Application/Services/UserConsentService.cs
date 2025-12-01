using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing user consents.
/// </summary>
public sealed class UserConsentService
{
    private readonly IUserConsentRepository _repository;

    public UserConsentService(IUserConsentRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all consents for a user.
    /// </summary>
    public async Task<IReadOnlyCollection<UserConsentDetailDto>> HandleAsync(
        GetUserConsentsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var consents = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        var results = new List<UserConsentDetailDto>();

        foreach (var consent in consents)
        {
            var consentType = await _repository.GetConsentTypeByIdAsync(consent.ConsentTypeId, cancellationToken);
            var consentVersion = await _repository.GetConsentVersionByIdAsync(consent.ConsentVersionId, cancellationToken);

            if (consentType is null || consentVersion is null)
            {
                continue;
            }

            var currentVersion = await _repository.GetCurrentVersionAsync(consent.ConsentTypeId, cancellationToken);

            results.Add(new UserConsentDetailDto(
                consent.Id,
                consent.UserId,
                MapToConsentTypeDto(consentType, currentVersion),
                MapToConsentVersionDto(consentVersion),
                consent.IsGranted,
                consent.ConsentedAt,
                consent.WithdrawnAt,
                consent.Source,
                consent.IsActive));
        }

        return results.OrderBy(c => c.ConsentType.DisplayOrder).ToList();
    }

    /// <summary>
    /// Gets all consent types.
    /// </summary>
    public async Task<IReadOnlyCollection<ConsentTypeDto>> HandleAsync(
        GetConsentTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var consentTypes = query.IncludeInactive
            ? await _repository.GetAllConsentTypesAsync(cancellationToken)
            : await _repository.GetActiveConsentTypesAsync(cancellationToken);

        var results = new List<ConsentTypeDto>();

        foreach (var consentType in consentTypes)
        {
            var currentVersion = await _repository.GetCurrentVersionAsync(consentType.Id, cancellationToken);
            results.Add(MapToConsentTypeDto(consentType, currentVersion));
        }

        return results.OrderBy(c => c.DisplayOrder).ToList();
    }

    /// <summary>
    /// Gets all active consent types for presenting to users.
    /// </summary>
    public async Task<IReadOnlyCollection<ConsentTypeDto>> HandleAsync(
        GetActiveConsentTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var consentTypes = await _repository.GetActiveConsentTypesAsync(cancellationToken);
        var results = new List<ConsentTypeDto>();

        foreach (var consentType in consentTypes)
        {
            var currentVersion = await _repository.GetCurrentVersionAsync(consentType.Id, cancellationToken);
            results.Add(MapToConsentTypeDto(consentType, currentVersion));
        }

        return results.OrderBy(c => c.DisplayOrder).ToList();
    }

    /// <summary>
    /// Checks if a user has active consent for a specific type.
    /// </summary>
    public async Task<ConsentEligibilityDto> HandleAsync(
        CheckUserConsentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var consent = await _repository.GetActiveConsentByCodeAsync(
            query.UserId, query.ConsentTypeCode, cancellationToken);

        if (consent is null || !consent.IsActive)
        {
            return new ConsentEligibilityDto(
                HasConsent: false,
                ConsentTypeCode: query.ConsentTypeCode,
                ConsentedAt: null,
                ConsentVersion: null);
        }

        var version = await _repository.GetConsentVersionByIdAsync(consent.ConsentVersionId, cancellationToken);

        return new ConsentEligibilityDto(
            HasConsent: true,
            ConsentTypeCode: query.ConsentTypeCode,
            ConsentedAt: consent.ConsentedAt,
            ConsentVersion: version?.Version);
    }

    /// <summary>
    /// Gets audit logs for a user's consents.
    /// </summary>
    public async Task<IReadOnlyCollection<UserConsentAuditLogDto>> HandleAsync(
        GetUserConsentAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var auditLogs = await _repository.GetAuditLogsByUserIdAsync(query.UserId, cancellationToken);
        var results = new List<UserConsentAuditLogDto>();

        foreach (var log in auditLogs)
        {
            var version = await _repository.GetConsentVersionByIdAsync(log.ConsentVersionId, cancellationToken);

            results.Add(new UserConsentAuditLogDto(
                log.Id,
                log.UserConsentId,
                log.UserId,
                log.Action.ToString(),
                version?.Version ?? "Unknown",
                log.Source,
                log.CreatedAt));
        }

        return results.OrderByDescending(l => l.CreatedAt).ToList();
    }

    /// <summary>
    /// Gets a consent type by ID.
    /// </summary>
    public async Task<ConsentTypeDto?> HandleAsync(
        GetConsentTypeByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var consentType = await _repository.GetConsentTypeByIdAsync(query.Id, cancellationToken);
        if (consentType is null)
        {
            return null;
        }

        var currentVersion = await _repository.GetCurrentVersionAsync(consentType.Id, cancellationToken);
        return MapToConsentTypeDto(consentType, currentVersion);
    }

    /// <summary>
    /// Gets consent versions for a consent type.
    /// </summary>
    public async Task<IReadOnlyCollection<ConsentVersionDto>> HandleAsync(
        GetConsentVersionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var versions = await _repository.GetVersionsByConsentTypeAsync(query.ConsentTypeId, cancellationToken);
        return versions.Select(MapToConsentVersionDto).OrderByDescending(v => v.EffectiveFrom).ToList();
    }

    /// <summary>
    /// Gets users with active consent for a specific type.
    /// </summary>
    public async Task<IReadOnlyCollection<Guid>> HandleAsync(
        GetUsersWithConsentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _repository.GetUsersWithActiveConsentAsync(query.ConsentTypeCode, cancellationToken);
    }

    /// <summary>
    /// Records a user's consent decision.
    /// </summary>
    public async Task<UserConsentResultDto> HandleAsync(
        RecordUserConsentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var consentType = await _repository.GetConsentTypeByIdAsync(command.ConsentTypeId, cancellationToken);
        if (consentType is null)
        {
            return UserConsentResultDto.Failed("Consent type not found.");
        }

        if (!consentType.IsActive)
        {
            return UserConsentResultDto.Failed("Consent type is not active.");
        }

        var currentVersion = await _repository.GetCurrentVersionAsync(command.ConsentTypeId, cancellationToken);
        if (currentVersion is null)
        {
            return UserConsentResultDto.Failed("No active consent version found.");
        }

        // Check if user already has an active consent for this type
        var existingConsent = await _repository.GetActiveConsentAsync(
            command.UserId, command.ConsentTypeId, cancellationToken);

        if (existingConsent is not null && existingConsent.IsActive)
        {
            // If granting and already granted with same version, no change needed
            if (command.IsGranted && existingConsent.ConsentVersionId == currentVersion.Id)
            {
                return UserConsentResultDto.Succeeded(
                    MapToUserConsentDto(existingConsent, consentType, currentVersion),
                    "Consent is already active.");
            }

            // Withdraw existing consent before recording new one
            existingConsent.Withdraw();
            _repository.Update(existingConsent);

            var withdrawalLog = new UserConsentAuditLog(
                existingConsent.Id,
                command.UserId,
                UserConsentAuditAction.Withdrawn,
                existingConsent.ConsentVersionId,
                command.Source,
                command.IpAddress,
                command.UserAgent);

            await _repository.AddAuditLogAsync(withdrawalLog, cancellationToken);
        }

        // Create new consent record
        var consent = new UserConsent(
            command.UserId,
            command.ConsentTypeId,
            currentVersion.Id,
            command.IsGranted,
            command.Source,
            command.IpAddress,
            command.UserAgent);

        await _repository.AddAsync(consent, cancellationToken);

        var auditAction = existingConsent?.IsActive == true 
            ? UserConsentAuditAction.Renewed 
            : UserConsentAuditAction.Granted;

        if (!command.IsGranted)
        {
            auditAction = UserConsentAuditAction.Withdrawn;
        }

        var auditLog = new UserConsentAuditLog(
            consent.Id,
            command.UserId,
            auditAction,
            currentVersion.Id,
            command.Source,
            command.IpAddress,
            command.UserAgent);

        await _repository.AddAuditLogAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return UserConsentResultDto.Succeeded(
            MapToUserConsentDto(consent, consentType, currentVersion),
            command.IsGranted ? "Consent granted successfully." : "Consent decision recorded.");
    }

    /// <summary>
    /// Records multiple consent decisions at once.
    /// </summary>
    public async Task<BulkConsentResultDto> HandleAsync(
        RecordBulkUserConsentsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Decisions.Count == 0)
        {
            return BulkConsentResultDto.Failed("No consent decisions provided.");
        }

        var results = new List<UserConsentDto>();
        var errors = new List<string>();

        foreach (var decision in command.Decisions)
        {
            var result = await HandleAsync(
                new RecordUserConsentCommand(
                    command.UserId,
                    decision.ConsentTypeId,
                    decision.IsGranted,
                    command.Source,
                    command.IpAddress,
                    command.UserAgent),
                cancellationToken);

            if (result.Success && result.Consent is not null)
            {
                results.Add(result.Consent);
            }
            else
            {
                errors.AddRange(result.Errors);
            }
        }

        if (errors.Count > 0)
        {
            return BulkConsentResultDto.Failed(errors);
        }

        return BulkConsentResultDto.Succeeded(results, "All consent decisions recorded successfully.");
    }

    /// <summary>
    /// Withdraws a user's consent.
    /// </summary>
    public async Task<UserConsentResultDto> HandleAsync(
        WithdrawUserConsentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var consentType = await _repository.GetConsentTypeByIdAsync(command.ConsentTypeId, cancellationToken);
        if (consentType is null)
        {
            return UserConsentResultDto.Failed("Consent type not found.");
        }

        if (consentType.IsRequired)
        {
            return UserConsentResultDto.Failed("Required consents cannot be withdrawn.");
        }

        var existingConsent = await _repository.GetActiveConsentAsync(
            command.UserId, command.ConsentTypeId, cancellationToken);

        if (existingConsent is null || !existingConsent.IsActive)
        {
            return UserConsentResultDto.Failed("No active consent found to withdraw.");
        }

        existingConsent.Withdraw();
        _repository.Update(existingConsent);

        var auditLog = new UserConsentAuditLog(
            existingConsent.Id,
            command.UserId,
            UserConsentAuditAction.Withdrawn,
            existingConsent.ConsentVersionId,
            command.Source,
            command.IpAddress,
            command.UserAgent);

        await _repository.AddAuditLogAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var version = await _repository.GetConsentVersionByIdAsync(
            existingConsent.ConsentVersionId, cancellationToken);

        return UserConsentResultDto.Succeeded(
            MapToUserConsentDto(existingConsent, consentType, version),
            "Consent withdrawn successfully.");
    }

    /// <summary>
    /// Creates a new consent type.
    /// </summary>
    public async Task<ConsentTypeDto?> HandleAsync(
        CreateConsentTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if consent type with same code already exists
        var existing = await _repository.GetConsentTypeByCodeAsync(command.Code, cancellationToken);
        if (existing is not null)
        {
            return null; // Already exists
        }

        var consentType = new ConsentType(
            command.Code,
            command.Name,
            command.Description,
            command.AllowPreselection,
            command.IsRequired,
            command.DisplayOrder);

        await _repository.AddConsentTypeAsync(consentType, cancellationToken);

        ConsentVersion? version = null;
        if (!string.IsNullOrWhiteSpace(command.InitialConsentText))
        {
            version = new ConsentVersion(
                consentType.Id,
                "1.0",
                command.InitialConsentText,
                DateTime.UtcNow);

            await _repository.AddConsentVersionAsync(version, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return MapToConsentTypeDto(consentType, version);
    }

    /// <summary>
    /// Updates a consent type.
    /// </summary>
    public async Task<ConsentTypeDto?> HandleAsync(
        UpdateConsentTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var consentType = await _repository.GetConsentTypeByIdAsync(command.Id, cancellationToken);
        if (consentType is null)
        {
            return null;
        }

        consentType.Update(
            command.Name,
            command.Description,
            command.AllowPreselection,
            command.IsRequired,
            command.DisplayOrder);

        _repository.UpdateConsentType(consentType);
        await _repository.SaveChangesAsync(cancellationToken);

        var currentVersion = await _repository.GetCurrentVersionAsync(consentType.Id, cancellationToken);
        return MapToConsentTypeDto(consentType, currentVersion);
    }

    /// <summary>
    /// Creates a new consent version.
    /// </summary>
    public async Task<ConsentVersionDto?> HandleAsync(
        CreateConsentVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var consentType = await _repository.GetConsentTypeByIdAsync(command.ConsentTypeId, cancellationToken);
        if (consentType is null)
        {
            return null;
        }

        // Supersede the current version if one exists
        var currentVersion = await _repository.GetCurrentVersionAsync(command.ConsentTypeId, cancellationToken);
        if (currentVersion is not null)
        {
            currentVersion.Supersede(command.EffectiveFrom);
            _repository.UpdateConsentVersion(currentVersion);
        }

        var version = new ConsentVersion(
            command.ConsentTypeId,
            command.Version,
            command.ConsentText,
            command.EffectiveFrom);

        await _repository.AddConsentVersionAsync(version, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return MapToConsentVersionDto(version);
    }

    private static ConsentTypeDto MapToConsentTypeDto(ConsentType consentType, ConsentVersion? currentVersion)
    {
        return new ConsentTypeDto(
            consentType.Id,
            consentType.Code,
            consentType.Name,
            consentType.Description,
            consentType.IsActive,
            consentType.AllowPreselection,
            consentType.IsRequired,
            consentType.DisplayOrder,
            currentVersion is not null ? MapToConsentVersionDto(currentVersion) : null);
    }

    private static ConsentVersionDto MapToConsentVersionDto(ConsentVersion version)
    {
        return new ConsentVersionDto(
            version.Id,
            version.ConsentTypeId,
            version.Version,
            version.ConsentText,
            version.IsCurrent,
            version.EffectiveFrom,
            version.EffectiveTo);
    }

    private static UserConsentDto MapToUserConsentDto(
        UserConsent consent,
        ConsentType consentType,
        ConsentVersion? version)
    {
        return new UserConsentDto(
            consent.Id,
            consent.UserId,
            consent.ConsentTypeId,
            consentType.Code,
            consentType.Name,
            consent.ConsentVersionId,
            version?.Version ?? "Unknown",
            consent.IsGranted,
            consent.ConsentedAt,
            consent.WithdrawnAt,
            consent.Source,
            consent.IsActive);
    }
}
