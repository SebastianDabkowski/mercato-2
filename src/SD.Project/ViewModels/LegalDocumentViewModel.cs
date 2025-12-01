using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying legal documents in admin screens.
/// </summary>
public class LegalDocumentViewModel
{
    public Guid Id { get; set; }
    public LegalDocumentType DocumentType { get; set; }
    public string DocumentTypeName { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Current version info
    public string? CurrentVersionNumber { get; set; }
    public DateTime? CurrentVersionEffectiveFrom { get; set; }

    // Scheduled version info
    public string? ScheduledVersionNumber { get; set; }
    public DateTime? ScheduledVersionEffectiveFrom { get; set; }
    public string? ScheduledVersionChangesSummary { get; set; }
}

/// <summary>
/// View model for displaying legal document versions.
/// </summary>
public class LegalDocumentVersionViewModel
{
    public Guid Id { get; set; }
    public Guid LegalDocumentId { get; set; }
    public string VersionNumber { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string? ChangesSummary { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPublished { get; set; }
    public bool IsCurrentlyActive { get; set; }
    public bool IsScheduled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string StatusDisplay => GetStatusDisplay();

    public string StatusBadgeClass => GetStatusBadgeClass();

    private string GetStatusDisplay()
    {
        if (IsCurrentlyActive) return "Active";
        if (IsScheduled) return "Scheduled";
        if (!IsPublished) return "Draft";
        return "Superseded";
    }

    private string GetStatusBadgeClass()
    {
        if (IsCurrentlyActive) return "bg-success";
        if (IsScheduled) return "bg-info";
        if (!IsPublished) return "bg-warning text-dark";
        return "bg-secondary";
    }
}
