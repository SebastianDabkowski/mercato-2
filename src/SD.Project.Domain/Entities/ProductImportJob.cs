namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a product catalog import job that tracks the status and results of an import operation.
/// </summary>
public class ProductImportJob
{
    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid InitiatedByUserId { get; private set; }
    public string FileName { get; private set; } = default!;
    public ProductImportJobStatus Status { get; private set; }
    public int TotalRows { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public int CreatedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public string? ErrorReport { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private ProductImportJob()
    {
        // EF Core constructor
    }

    public ProductImportJob(Guid id, Guid storeId, Guid initiatedByUserId, string fileName, int totalRows)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required", nameof(fileName));
        }

        if (totalRows < 0)
        {
            throw new ArgumentException("Total rows cannot be negative", nameof(totalRows));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StoreId = storeId;
        InitiatedByUserId = initiatedByUserId;
        FileName = fileName;
        TotalRows = totalRows;
        Status = ProductImportJobStatus.Pending;
        SuccessCount = 0;
        FailureCount = 0;
        CreatedCount = 0;
        UpdatedCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Starts processing the import job.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != ProductImportJobStatus.Pending)
        {
            throw new InvalidOperationException("Can only start processing a pending job.");
        }

        Status = ProductImportJobStatus.Processing;
    }

    /// <summary>
    /// Marks the job as completed with the final statistics.
    /// </summary>
    public void Complete(int successCount, int failureCount, int createdCount, int updatedCount, string? errorReport)
    {
        if (Status != ProductImportJobStatus.Processing)
        {
            throw new InvalidOperationException("Can only complete a processing job.");
        }

        SuccessCount = successCount;
        FailureCount = failureCount;
        CreatedCount = createdCount;
        UpdatedCount = updatedCount;
        ErrorReport = errorReport;
        CompletedAt = DateTime.UtcNow;

        Status = failureCount > 0 ? ProductImportJobStatus.CompletedWithErrors : ProductImportJobStatus.Completed;
    }

    /// <summary>
    /// Marks the job as failed due to an error.
    /// </summary>
    public void Fail(string errorMessage)
    {
        Status = ProductImportJobStatus.Failed;
        ErrorReport = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the import job.
    /// </summary>
    public void Cancel()
    {
        if (Status == ProductImportJobStatus.Completed || Status == ProductImportJobStatus.CompletedWithErrors)
        {
            throw new InvalidOperationException("Cannot cancel a completed job.");
        }

        Status = ProductImportJobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }
}
