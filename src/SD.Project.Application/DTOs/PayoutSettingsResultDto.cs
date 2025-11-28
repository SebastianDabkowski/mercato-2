namespace SD.Project.Application.DTOs;

/// <summary>
/// Result DTO for payout settings operations.
/// </summary>
public sealed record PayoutSettingsResultDto(
    bool Success,
    string? Message,
    IReadOnlyCollection<string> Errors,
    PayoutSettingsDto? Settings)
{
    public static PayoutSettingsResultDto Succeeded(string message, PayoutSettingsDto? settings = null)
        => new(true, message, Array.Empty<string>(), settings);

    public static PayoutSettingsResultDto Failed(string error)
        => new(false, null, new[] { error }, null);

    public static PayoutSettingsResultDto Failed(IReadOnlyCollection<string> errors)
        => new(false, null, errors, null);
}
