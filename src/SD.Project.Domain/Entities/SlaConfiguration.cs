namespace SD.Project.Domain.Entities;

/// <summary>
/// Type of SLA breach.
/// </summary>
public enum SlaBreachType
{
    /// <summary>First response deadline was exceeded.</summary>
    FirstResponse,
    /// <summary>Resolution deadline was exceeded.</summary>
    Resolution
}

/// <summary>
/// Category for SLA configuration (determines which thresholds apply).
/// </summary>
public enum SlaCaseCategory
{
    /// <summary>Default SLA thresholds for all cases.</summary>
    Default,
    /// <summary>Return request cases.</summary>
    Return,
    /// <summary>Complaint cases.</summary>
    Complaint
}

/// <summary>
/// Represents SLA configuration for case handling.
/// Stores thresholds for first response and resolution deadlines.
/// Configurable per case category (Return, Complaint, or Default).
/// </summary>
public class SlaConfiguration
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The case category this configuration applies to.
    /// </summary>
    public SlaCaseCategory Category { get; private set; }

    /// <summary>
    /// Hours allowed for seller's first response from case creation.
    /// </summary>
    public int FirstResponseHours { get; private set; }

    /// <summary>
    /// Hours allowed for case resolution from case creation.
    /// </summary>
    public int ResolutionHours { get; private set; }

    /// <summary>
    /// Whether this SLA configuration is currently active.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// When this configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Optional description for admin reference.
    /// </summary>
    public string? Description { get; private set; }

    private SlaConfiguration()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new SLA configuration.
    /// </summary>
    /// <param name="category">The case category this applies to.</param>
    /// <param name="firstResponseHours">Hours allowed for first response.</param>
    /// <param name="resolutionHours">Hours allowed for resolution.</param>
    /// <param name="description">Optional description.</param>
    public SlaConfiguration(
        SlaCaseCategory category,
        int firstResponseHours,
        int resolutionHours,
        string? description = null)
    {
        if (firstResponseHours <= 0)
        {
            throw new ArgumentException("First response hours must be greater than zero.", nameof(firstResponseHours));
        }

        if (resolutionHours <= 0)
        {
            throw new ArgumentException("Resolution hours must be greater than zero.", nameof(resolutionHours));
        }

        if (firstResponseHours >= resolutionHours)
        {
            throw new ArgumentException("First response hours must be less than resolution hours.", nameof(firstResponseHours));
        }

        Id = Guid.NewGuid();
        Category = category;
        FirstResponseHours = firstResponseHours;
        ResolutionHours = resolutionHours;
        IsEnabled = true;
        Description = description?.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the SLA thresholds.
    /// </summary>
    /// <param name="firstResponseHours">New hours allowed for first response.</param>
    /// <param name="resolutionHours">New hours allowed for resolution.</param>
    /// <param name="description">Optional updated description.</param>
    public void UpdateThresholds(int firstResponseHours, int resolutionHours, string? description = null)
    {
        if (firstResponseHours <= 0)
        {
            throw new ArgumentException("First response hours must be greater than zero.", nameof(firstResponseHours));
        }

        if (resolutionHours <= 0)
        {
            throw new ArgumentException("Resolution hours must be greater than zero.", nameof(resolutionHours));
        }

        if (firstResponseHours >= resolutionHours)
        {
            throw new ArgumentException("First response hours must be less than resolution hours.", nameof(firstResponseHours));
        }

        FirstResponseHours = firstResponseHours;
        ResolutionHours = resolutionHours;
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables the SLA configuration.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the SLA configuration.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the first response deadline from a given start time.
    /// </summary>
    /// <param name="startTime">The case creation time.</param>
    /// <returns>The deadline for first response.</returns>
    public DateTime CalculateFirstResponseDeadline(DateTime startTime)
    {
        return startTime.AddHours(FirstResponseHours);
    }

    /// <summary>
    /// Calculates the resolution deadline from a given start time.
    /// </summary>
    /// <param name="startTime">The case creation time.</param>
    /// <returns>The deadline for resolution.</returns>
    public DateTime CalculateResolutionDeadline(DateTime startTime)
    {
        return startTime.AddHours(ResolutionHours);
    }
}
