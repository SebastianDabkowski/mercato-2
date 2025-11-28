namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the result of an internal user operation.
/// </summary>
public sealed record InternalUserResultDto(
    bool Success,
    string? Message,
    Guid? InternalUserId,
    IReadOnlyList<string>? Errors = null)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static InternalUserResultDto Succeeded(Guid internalUserId, string message)
        => new(true, message, internalUserId);

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    public static InternalUserResultDto Failed(string error)
        => new(false, null, null, new[] { error });

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    public static InternalUserResultDto Failed(IEnumerable<string> errors)
        => new(false, null, null, errors.ToList());
}
