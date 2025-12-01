namespace SD.Project.ViewModels;

/// <summary>
/// View model for a data processing activity row in the registry list view.
/// </summary>
public sealed class DataProcessingActivityViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = string.Empty;
    public string Purpose { get; init; } = default!;
    public string LegalBasis { get; init; } = default!;
    public string DataCategories { get; init; } = default!;
    public string DataSubjects { get; init; } = default!;
    public string Processors { get; init; } = string.Empty;
    public string RetentionPeriod { get; init; } = default!;
    public string? InternationalTransfers { get; init; }
    public string? SecurityMeasures { get; init; }
    public bool IsActive { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string? CreatedByUserName { get; init; }
    public Guid? LastModifiedByUserId { get; init; }
    public string? LastModifiedByUserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the status badge CSS class.
    /// </summary>
    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";

    /// <summary>
    /// Gets the status display text.
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Archived";

    /// <summary>
    /// Gets the created info for display.
    /// </summary>
    public string CreatedDisplay => $"{CreatedByUserName ?? "Unknown"} on {CreatedAt:MMM d, yyyy 'at' h:mm tt}";

    /// <summary>
    /// Gets the last modified info for display.
    /// </summary>
    public string LastModifiedDisplay => LastModifiedByUserName != null
        ? $"{LastModifiedByUserName} on {UpdatedAt:MMM d, yyyy 'at' h:mm tt}"
        : $"{CreatedByUserName ?? "Unknown"} on {CreatedAt:MMM d, yyyy 'at' h:mm tt}";

    /// <summary>
    /// Gets the truncated purpose for table display.
    /// </summary>
    public string TruncatedPurpose => Purpose.Length > 100 ? $"{Purpose[..97]}..." : Purpose;

    /// <summary>
    /// Gets the truncated data categories for table display.
    /// </summary>
    public string TruncatedDataCategories => DataCategories.Length > 100 ? $"{DataCategories[..97]}..." : DataCategories;

    /// <summary>
    /// Gets the truncated data subjects for table display.
    /// </summary>
    public string TruncatedDataSubjects => DataSubjects.Length > 80 ? $"{DataSubjects[..77]}..." : DataSubjects;

    /// <summary>
    /// Gets whether there are international transfers.
    /// </summary>
    public bool HasInternationalTransfers => !string.IsNullOrWhiteSpace(InternationalTransfers);
}

/// <summary>
/// View model for a data processing activity audit log entry.
/// </summary>
public sealed class DataProcessingActivityAuditLogViewModel
{
    public Guid Id { get; init; }
    public Guid DataProcessingActivityId { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = default!;
    public string Action { get; init; } = default!;
    public string? ChangeReason { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the badge CSS class for the action type.
    /// </summary>
    public string ActionBadgeClass => Action switch
    {
        "Created" => "bg-success",
        "Updated" => "bg-primary",
        "Archived" => "bg-warning text-dark",
        "Reactivated" => "bg-info",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the formatted timestamp.
    /// </summary>
    public string TimestampDisplay => CreatedAt.ToString("MMM d, yyyy 'at' h:mm tt");
}
