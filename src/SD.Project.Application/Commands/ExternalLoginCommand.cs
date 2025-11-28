using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to process an external login (OAuth) request.
/// </summary>
public sealed record ExternalLoginCommand(
    string Email,
    ExternalLoginProvider Provider,
    string ExternalId,
    string FirstName,
    string LastName);
