namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of a user data export request.
/// </summary>
public enum UserDataExportStatus
{
    /// <summary>Export request has been submitted and is pending processing.</summary>
    Pending,
    /// <summary>Export is currently being generated.</summary>
    Processing,
    /// <summary>Export has been completed and is ready for download.</summary>
    Completed,
    /// <summary>Export generation failed.</summary>
    Failed,
    /// <summary>Export has expired and is no longer available for download.</summary>
    Expired
}

/// <summary>
/// Represents a GDPR data export request from a user.
/// Tracks the lifecycle of data export requests for compliance with the right of access.
/// </summary>
public class UserDataExport
{
    /// <summary>
    /// Unique identifier for this export request.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user who requested the export.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Current status of the export request.
    /// </summary>
    public UserDataExportStatus Status { get; private set; }

    /// <summary>
    /// UTC timestamp when the export was requested.
    /// </summary>
    public DateTime RequestedAt { get; private set; }

    /// <summary>
    /// UTC timestamp when the export processing started.
    /// </summary>
    public DateTime? ProcessingStartedAt { get; private set; }

    /// <summary>
    /// UTC timestamp when the export was completed.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// UTC timestamp when the export expires and can no longer be downloaded.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// The exported data in JSON format.
    /// </summary>
    public string? ExportData { get; private set; }

    /// <summary>
    /// Error message if the export failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// The IP address from which the export was requested.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// The user agent string of the browser/client used for the request.
    /// </summary>
    public string? UserAgent { get; private set; }

    private UserDataExport()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new user data export request.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the export.</param>
    /// <param name="ipAddress">The IP address of the requester (optional).</param>
    /// <param name="userAgent">The user agent string (optional).</param>
    public UserDataExport(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Status = UserDataExportStatus.Pending;
        RequestedAt = DateTime.UtcNow;
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
    }

    /// <summary>
    /// Marks the export as processing.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != UserDataExportStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start processing export in status {Status}.");
        }

        Status = UserDataExportStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the export with the generated data.
    /// </summary>
    /// <param name="exportData">The exported data in JSON format.</param>
    /// <param name="expirationHours">Number of hours until the export expires (default: 72 hours).</param>
    public void Complete(string exportData, int expirationHours = 72)
    {
        if (Status != UserDataExportStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot complete export in status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(exportData))
        {
            throw new ArgumentException("Export data is required.", nameof(exportData));
        }

        Status = UserDataExportStatus.Completed;
        ExportData = exportData;
        CompletedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddHours(expirationHours);
    }

    /// <summary>
    /// Marks the export as failed with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the export failed.</param>
    public void Fail(string errorMessage)
    {
        if (Status != UserDataExportStatus.Pending && Status != UserDataExportStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot fail export in status {Status}.");
        }

        Status = UserDataExportStatus.Failed;
        ErrorMessage = errorMessage?.Trim();
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the export as expired.
    /// </summary>
    public void Expire()
    {
        if (Status != UserDataExportStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot expire export in status {Status}.");
        }

        Status = UserDataExportStatus.Expired;
        ExportData = null; // Clear the data to free up storage
    }

    /// <summary>
    /// Indicates whether the export is ready for download.
    /// </summary>
    public bool IsDownloadable => Status == UserDataExportStatus.Completed && 
                                   ExpiresAt.HasValue && 
                                   DateTime.UtcNow < ExpiresAt.Value;
}
