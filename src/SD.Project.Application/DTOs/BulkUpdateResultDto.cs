namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of a single product update in a bulk operation.
/// </summary>
/// <param name="ProductId">The ID of the product.</param>
/// <param name="ProductName">The name of the product.</param>
/// <param name="IsSuccess">Whether the update succeeded for this product.</param>
/// <param name="ErrorMessage">Error message if the update failed.</param>
/// <param name="OldPrice">The previous price amount.</param>
/// <param name="NewPrice">The new price amount.</param>
/// <param name="OldStock">The previous stock level.</param>
/// <param name="NewStock">The new stock level.</param>
public sealed record BulkUpdateItemResultDto(
    Guid ProductId,
    string ProductName,
    bool IsSuccess,
    string? ErrorMessage,
    decimal? OldPrice,
    decimal? NewPrice,
    int? OldStock,
    int? NewStock);

/// <summary>
/// Represents the overall result of a bulk price and stock update operation.
/// </summary>
public sealed class BulkUpdateResultDto
{
    public bool IsSuccess { get; private init; }
    public int TotalCount { get; private init; }
    public int SuccessCount { get; private init; }
    public int FailureCount { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();
    public IReadOnlyList<BulkUpdateItemResultDto> Results { get; private init; } = Array.Empty<BulkUpdateItemResultDto>();

    private BulkUpdateResultDto() { }

    /// <summary>
    /// Creates a successful result with all item results.
    /// </summary>
    public static BulkUpdateResultDto Succeeded(IReadOnlyList<BulkUpdateItemResultDto> results)
    {
        var successCount = results.Count(r => r.IsSuccess);
        return new BulkUpdateResultDto
        {
            IsSuccess = true,
            TotalCount = results.Count,
            SuccessCount = successCount,
            FailureCount = results.Count - successCount,
            Results = results,
            Errors = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a failed result due to validation or authorization errors.
    /// </summary>
    public static BulkUpdateResultDto Failed(params string[] errors)
    {
        return new BulkUpdateResultDto
        {
            IsSuccess = false,
            TotalCount = 0,
            SuccessCount = 0,
            FailureCount = 0,
            Results = Array.Empty<BulkUpdateItemResultDto>(),
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a failed result from a list of errors.
    /// </summary>
    public static BulkUpdateResultDto Failed(IReadOnlyList<string> errors)
    {
        return new BulkUpdateResultDto
        {
            IsSuccess = false,
            TotalCount = 0,
            SuccessCount = 0,
            FailureCount = 0,
            Results = Array.Empty<BulkUpdateItemResultDto>(),
            Errors = errors
        };
    }
}
