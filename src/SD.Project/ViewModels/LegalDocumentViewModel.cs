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

    public string StatusDisplay => IsCurrentlyActive ? "Active" : 
                                   IsScheduled ? "Scheduled" : 
                                   !IsPublished ? "Draft" : 
                                   "Superseded";

    public string StatusBadgeClass => IsCurrentlyActive ? "bg-success" :
                                      IsScheduled ? "bg-info" :
                                      !IsPublished ? "bg-warning text-dark" :
                                      "bg-secondary";
}
