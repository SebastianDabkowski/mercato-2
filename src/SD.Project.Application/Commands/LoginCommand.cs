namespace SD.Project.Application.Commands;

/// <summary>
/// Command to authenticate a user with email and password.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null);
