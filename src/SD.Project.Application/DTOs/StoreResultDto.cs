namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the result of a store operation.
/// </summary>
public sealed record StoreResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public StoreDto? Store { get; init; }

    public static StoreResultDto Succeeded(StoreDto store, string message = "Store updated successfully.")
    {
        return new StoreResultDto
        {
            Success = true,
            Message = message,
            Store = store
        };
    }

    public static StoreResultDto Failed(string error)
    {
        return new StoreResultDto
        {
            Success = false,
            Errors = new[] { error }
        };
    }

    public static StoreResultDto Failed(IReadOnlyList<string> errors)
    {
        return new StoreResultDto
        {
            Success = false,
            Errors = errors
        };
    }
}
