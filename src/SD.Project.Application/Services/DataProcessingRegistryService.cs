using System.Globalization;
using System.Text;
using System.Text.Json;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing data processing activities (GDPR Art. 30 registry).
/// </summary>
public sealed class DataProcessingRegistryService
{
    private readonly IDataProcessingActivityRepository _repository;
    private readonly IUserRepository _userRepository;

    public DataProcessingRegistryService(
        IDataProcessingActivityRepository repository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Retrieves all data processing activities.
    /// </summary>
    public async Task<IReadOnlyCollection<DataProcessingActivityDto>> HandleAsync(
        GetAllDataProcessingActivitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var activities = query.IncludeArchived
            ? await _repository.GetAllAsync(cancellationToken)
            : await _repository.GetActiveAsync(cancellationToken);

        var userIds = activities
            .SelectMany(a => new[] { a.CreatedByUserId, a.LastModifiedByUserId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Concat(activities.Select(a => a.CreatedByUserId))
            .Distinct()
            .ToList();

        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNameLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        return activities
            .OrderByDescending(a => a.UpdatedAt)
            .Select(a => MapToDto(a, userNameLookup))
            .ToList();
    }

    /// <summary>
    /// Retrieves a data processing activity by ID.
    /// </summary>
    public async Task<DataProcessingActivityDto?> HandleAsync(
        GetDataProcessingActivityByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var activity = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (activity is null)
        {
            return null;
        }

        var userIds = new List<Guid> { activity.CreatedByUserId };
        if (activity.LastModifiedByUserId.HasValue)
        {
            userIds.Add(activity.LastModifiedByUserId.Value);
        }

        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNameLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        return MapToDto(activity, userNameLookup);
    }

    /// <summary>
    /// Retrieves audit logs for a data processing activity.
    /// </summary>
    public async Task<IReadOnlyCollection<DataProcessingActivityAuditLogDto>> HandleAsync(
        GetDataProcessingActivityAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var auditLogs = await _repository.GetAuditLogsAsync(query.DataProcessingActivityId, cancellationToken);

        var userIds = auditLogs.Select(a => a.UserId).Distinct().ToList();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNameLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        return auditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new DataProcessingActivityAuditLogDto(
                a.Id,
                a.DataProcessingActivityId,
                a.UserId,
                userNameLookup.TryGetValue(a.UserId, out var name) ? name : "Unknown User",
                a.Action.ToString(),
                a.ChangeReason,
                a.CreatedAt))
            .ToList();
    }

    /// <summary>
    /// Creates a new data processing activity.
    /// </summary>
    public async Task<DataProcessingActivityResultDto> HandleAsync(
        CreateDataProcessingActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateCommand(command);
        if (validationErrors.Count > 0)
        {
            return DataProcessingActivityResultDto.Failed(validationErrors);
        }

        try
        {
            var activity = new DataProcessingActivity(
                command.Name,
                command.Purpose,
                command.LegalBasis,
                command.DataCategories,
                command.DataSubjects,
                command.RetentionPeriod,
                command.CreatedByUserId,
                command.Description,
                command.Processors,
                command.InternationalTransfers,
                command.SecurityMeasures);

            await _repository.AddAsync(activity, cancellationToken);

            var auditLog = new DataProcessingActivityAuditLog(
                activity.Id,
                command.CreatedByUserId,
                DataProcessingActivityAuditAction.Created,
                SerializeActivityState(activity));

            await _repository.AddAuditLogAsync(auditLog, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var user = await _userRepository.GetByIdAsync(command.CreatedByUserId, cancellationToken);
            var userNameLookup = user is not null
                ? new Dictionary<Guid, string> { { user.Id, $"{user.FirstName} {user.LastName}" } }
                : new Dictionary<Guid, string>();

            return DataProcessingActivityResultDto.Succeeded(
                MapToDto(activity, userNameLookup),
                "Data processing activity created successfully.");
        }
        catch (ArgumentException ex)
        {
            return DataProcessingActivityResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing data processing activity.
    /// </summary>
    public async Task<DataProcessingActivityResultDto> HandleAsync(
        UpdateDataProcessingActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var activity = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (activity is null)
        {
            return DataProcessingActivityResultDto.Failed("Data processing activity not found.");
        }

        var validationErrors = ValidateUpdateCommand(command);
        if (validationErrors.Count > 0)
        {
            return DataProcessingActivityResultDto.Failed(validationErrors);
        }

        try
        {
            var previousState = SerializeActivityState(activity);

            activity.Update(
                command.Name,
                command.Purpose,
                command.LegalBasis,
                command.DataCategories,
                command.DataSubjects,
                command.RetentionPeriod,
                command.ModifiedByUserId,
                command.Description,
                command.Processors,
                command.InternationalTransfers,
                command.SecurityMeasures);

            _repository.Update(activity);

            var auditLog = new DataProcessingActivityAuditLog(
                activity.Id,
                command.ModifiedByUserId,
                DataProcessingActivityAuditAction.Updated,
                SerializeActivityState(activity),
                previousState,
                command.ChangeReason);

            await _repository.AddAuditLogAsync(auditLog, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var userIds = new List<Guid> { activity.CreatedByUserId };
            if (activity.LastModifiedByUserId.HasValue)
            {
                userIds.Add(activity.LastModifiedByUserId.Value);
            }

            var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
            var userNameLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

            return DataProcessingActivityResultDto.Succeeded(
                MapToDto(activity, userNameLookup),
                "Data processing activity updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return DataProcessingActivityResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Archives a data processing activity.
    /// </summary>
    public async Task<DataProcessingActivityResultDto> HandleAsync(
        ArchiveDataProcessingActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var activity = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (activity is null)
        {
            return DataProcessingActivityResultDto.Failed("Data processing activity not found.");
        }

        if (!activity.IsActive)
        {
            return DataProcessingActivityResultDto.Failed("Data processing activity is already archived.");
        }

        try
        {
            var previousState = SerializeActivityState(activity);

            activity.Archive(command.ModifiedByUserId);

            _repository.Update(activity);

            var auditLog = new DataProcessingActivityAuditLog(
                activity.Id,
                command.ModifiedByUserId,
                DataProcessingActivityAuditAction.Archived,
                SerializeActivityState(activity),
                previousState,
                command.ChangeReason);

            await _repository.AddAuditLogAsync(auditLog, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var userIds = new List<Guid> { activity.CreatedByUserId };
            if (activity.LastModifiedByUserId.HasValue)
            {
                userIds.Add(activity.LastModifiedByUserId.Value);
            }

            var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
            var userNameLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

            return DataProcessingActivityResultDto.Succeeded(
                MapToDto(activity, userNameLookup),
                "Data processing activity archived successfully.");
        }
        catch (ArgumentException ex)
        {
            return DataProcessingActivityResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Reactivates an archived data processing activity.
    /// </summary>
    public async Task<DataProcessingActivityResultDto> HandleAsync(
        ReactivateDataProcessingActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var activity = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (activity is null)
        {
            return DataProcessingActivityResultDto.Failed("Data processing activity not found.");
        }

        if (activity.IsActive)
        {
            return DataProcessingActivityResultDto.Failed("Data processing activity is already active.");
        }

        try
        {
            var previousState = SerializeActivityState(activity);

            activity.Reactivate(command.ModifiedByUserId);

            _repository.Update(activity);

            var auditLog = new DataProcessingActivityAuditLog(
                activity.Id,
                command.ModifiedByUserId,
                DataProcessingActivityAuditAction.Reactivated,
                SerializeActivityState(activity),
                previousState,
                command.ChangeReason);

            await _repository.AddAuditLogAsync(auditLog, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var userIds = new List<Guid> { activity.CreatedByUserId };
            if (activity.LastModifiedByUserId.HasValue)
            {
                userIds.Add(activity.LastModifiedByUserId.Value);
            }

            var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
            var userNameLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

            return DataProcessingActivityResultDto.Succeeded(
                MapToDto(activity, userNameLookup),
                "Data processing activity reactivated successfully.");
        }
        catch (ArgumentException ex)
        {
            return DataProcessingActivityResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Exports data processing activities to CSV format.
    /// </summary>
    public async Task<DataProcessingActivityExportResultDto> HandleAsync(
        ExportDataProcessingActivitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var activities = query.IncludeArchived
            ? await _repository.GetAllAsync(cancellationToken)
            : await _repository.GetActiveAsync(cancellationToken);

        var userIds = activities
            .SelectMany(a => new[] { a.CreatedByUserId, a.LastModifiedByUserId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Concat(activities.Select(a => a.CreatedByUserId))
            .Distinct()
            .ToList();

        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNameLookup = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var csv = new StringBuilder();
        csv.AppendLine("Name,Purpose,Legal Basis,Data Categories,Data Subjects,Processors,Retention Period,International Transfers,Security Measures,Status,Created By,Created At,Last Modified By,Last Modified At");

        foreach (var activity in activities.OrderByDescending(a => a.UpdatedAt))
        {
            var createdByName = userNameLookup.TryGetValue(activity.CreatedByUserId, out var creatorName) ? creatorName : "Unknown";
            var modifiedByName = activity.LastModifiedByUserId.HasValue && userNameLookup.TryGetValue(activity.LastModifiedByUserId.Value, out var modifierName)
                ? modifierName
                : string.Empty;

            csv.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}",
                EscapeCsv(activity.Name),
                EscapeCsv(activity.Purpose),
                EscapeCsv(activity.LegalBasis),
                EscapeCsv(activity.DataCategories),
                EscapeCsv(activity.DataSubjects),
                EscapeCsv(activity.Processors),
                EscapeCsv(activity.RetentionPeriod),
                EscapeCsv(activity.InternationalTransfers ?? string.Empty),
                EscapeCsv(activity.SecurityMeasures ?? string.Empty),
                activity.IsActive ? "Active" : "Archived",
                EscapeCsv(createdByName),
                activity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                EscapeCsv(modifiedByName),
                activity.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)));
        }

        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"data-processing-registry-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return DataProcessingActivityExportResultDto.Succeeded(content, fileName, "text/csv");
    }

    private static DataProcessingActivityDto MapToDto(
        DataProcessingActivity activity,
        Dictionary<Guid, string> userNameLookup)
    {
        return new DataProcessingActivityDto(
            activity.Id,
            activity.Name,
            activity.Description,
            activity.Purpose,
            activity.LegalBasis,
            activity.DataCategories,
            activity.DataSubjects,
            activity.Processors,
            activity.RetentionPeriod,
            activity.InternationalTransfers,
            activity.SecurityMeasures,
            activity.IsActive,
            activity.CreatedByUserId,
            userNameLookup.TryGetValue(activity.CreatedByUserId, out var creatorName) ? creatorName : null,
            activity.LastModifiedByUserId,
            activity.LastModifiedByUserId.HasValue && userNameLookup.TryGetValue(activity.LastModifiedByUserId.Value, out var modifierName) ? modifierName : null,
            activity.CreatedAt,
            activity.UpdatedAt);
    }

    private static string SerializeActivityState(DataProcessingActivity activity)
    {
        return JsonSerializer.Serialize(new
        {
            activity.Name,
            activity.Description,
            activity.Purpose,
            activity.LegalBasis,
            activity.DataCategories,
            activity.DataSubjects,
            activity.Processors,
            activity.RetentionPeriod,
            activity.InternationalTransfers,
            activity.SecurityMeasures,
            activity.IsActive
        });
    }

    private static IReadOnlyList<string> ValidateCommand(CreateDataProcessingActivityCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Name cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Purpose))
        {
            errors.Add("Purpose is required.");
        }
        else if (command.Purpose.Length > 1000)
        {
            errors.Add("Purpose cannot exceed 1000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.LegalBasis))
        {
            errors.Add("Legal basis is required.");
        }
        else if (command.LegalBasis.Length > 500)
        {
            errors.Add("Legal basis cannot exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.DataCategories))
        {
            errors.Add("Data categories are required.");
        }
        else if (command.DataCategories.Length > 1000)
        {
            errors.Add("Data categories cannot exceed 1000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.DataSubjects))
        {
            errors.Add("Data subjects are required.");
        }
        else if (command.DataSubjects.Length > 500)
        {
            errors.Add("Data subjects cannot exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.RetentionPeriod))
        {
            errors.Add("Retention period is required.");
        }
        else if (command.RetentionPeriod.Length > 500)
        {
            errors.Add("Retention period cannot exceed 500 characters.");
        }

        if (command.CreatedByUserId == Guid.Empty)
        {
            errors.Add("Creator user ID is required.");
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateUpdateCommand(UpdateDataProcessingActivityCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Name is required.");
        }
        else if (command.Name.Length > 200)
        {
            errors.Add("Name cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Purpose))
        {
            errors.Add("Purpose is required.");
        }
        else if (command.Purpose.Length > 1000)
        {
            errors.Add("Purpose cannot exceed 1000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.LegalBasis))
        {
            errors.Add("Legal basis is required.");
        }
        else if (command.LegalBasis.Length > 500)
        {
            errors.Add("Legal basis cannot exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.DataCategories))
        {
            errors.Add("Data categories are required.");
        }
        else if (command.DataCategories.Length > 1000)
        {
            errors.Add("Data categories cannot exceed 1000 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.DataSubjects))
        {
            errors.Add("Data subjects are required.");
        }
        else if (command.DataSubjects.Length > 500)
        {
            errors.Add("Data subjects cannot exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.RetentionPeriod))
        {
            errors.Add("Retention period is required.");
        }
        else if (command.RetentionPeriod.Length > 500)
        {
            errors.Add("Retention period cannot exceed 500 characters.");
        }

        if (command.ModifiedByUserId == Guid.Empty)
        {
            errors.Add("Modifier user ID is required.");
        }

        return errors;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // If the value contains a comma, double quote, or newline, wrap it in quotes and escape any internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
