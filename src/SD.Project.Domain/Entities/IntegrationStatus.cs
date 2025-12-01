namespace SD.Project.Domain.Entities;

/// <summary>
/// Status of an external integration.
/// </summary>
public enum IntegrationStatus
{
    /// <summary>Integration is active and operational.</summary>
    Active,

    /// <summary>Integration is temporarily disabled by admin.</summary>
    Disabled,

    /// <summary>Integration is experiencing connectivity issues.</summary>
    Unhealthy,

    /// <summary>Integration is not yet configured.</summary>
    Pending
}
