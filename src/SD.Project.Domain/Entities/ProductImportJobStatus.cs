namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of a product import job.
/// </summary>
public enum ProductImportJobStatus
{
    /// <summary>
    /// The import file has been validated and is waiting for confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The import is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// The import completed successfully with all rows processed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The import completed with some rows failing validation.
    /// </summary>
    CompletedWithErrors = 3,

    /// <summary>
    /// The import failed due to a system error.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The import was cancelled by the user.
    /// </summary>
    Cancelled = 5
}
