namespace SD.Project.ViewModels;

/// <summary>
/// View model for import job display.
/// </summary>
public record ImportJobViewModel(
    Guid Id,
    string FileName,
    string Status,
    string StatusClass,
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    int CreatedCount,
    int UpdatedCount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool HasErrorReport)
{
    public string StatusDisplay => Status switch
    {
        "Pending" => "Pending",
        "Processing" => "Processing",
        "Completed" => "Completed",
        "CompletedWithErrors" => "Completed with Errors",
        "Failed" => "Failed",
        "Cancelled" => "Cancelled",
        _ => Status
    };
}
