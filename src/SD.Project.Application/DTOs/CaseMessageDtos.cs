namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for a case message.
/// </summary>
public sealed record CaseMessageDto(
    Guid MessageId,
    Guid ReturnRequestId,
    Guid SenderId,
    string SenderRole,
    string SenderName,
    string Content,
    DateTime SentAt,
    bool IsRead,
    DateTime? ReadAt);

/// <summary>
/// Result DTO for sending a case message.
/// </summary>
public sealed record SendCaseMessageResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? MessageId = null);

/// <summary>
/// DTO for case message thread with metadata.
/// </summary>
public sealed record CaseMessageThreadDto(
    Guid ReturnRequestId,
    string CaseNumber,
    IReadOnlyList<CaseMessageDto> Messages,
    int UnreadCount);
