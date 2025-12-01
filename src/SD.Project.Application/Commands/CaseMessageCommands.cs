namespace SD.Project.Application.Commands;

/// <summary>
/// Command to send a message in a case messaging thread.
/// </summary>
public sealed record SendCaseMessageCommand(
    Guid ReturnRequestId,
    Guid SenderId,
    string SenderRole,
    string Content);

/// <summary>
/// Command to mark case messages as read.
/// </summary>
public sealed record MarkCaseMessagesReadCommand(
    Guid ReturnRequestId,
    Guid UserId,
    string UserRole);
