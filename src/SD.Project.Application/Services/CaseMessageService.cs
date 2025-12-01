using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for case messaging operations.
/// Handles sending messages within case threads and managing read status.
/// </summary>
public sealed class CaseMessageService
{
    private readonly ICaseMessageRepository _caseMessageRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public CaseMessageService(
        ICaseMessageRepository caseMessageRepository,
        IReturnRequestRepository returnRequestRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _caseMessageRepository = caseMessageRepository;
        _returnRequestRepository = returnRequestRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets messages for a case, with authorization check.
    /// </summary>
    public async Task<CaseMessageThreadDto?> HandleAsync(
        GetCaseMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Get the return request to verify access
        var returnRequest = await _returnRequestRepository.GetByIdAsync(query.ReturnRequestId, cancellationToken);
        if (returnRequest is null)
        {
            return null;
        }

        // Check authorization
        if (!await IsAuthorizedAsync(returnRequest, query.UserId, query.UserRole, cancellationToken))
        {
            return null;
        }

        // Parse recipient role for unread count
        var recipientRole = ParseSenderRole(query.UserRole);
        if (!recipientRole.HasValue)
        {
            return null;
        }

        // Get messages
        var messages = await _caseMessageRepository.GetByReturnRequestIdAsync(query.ReturnRequestId, cancellationToken);
        var unreadCount = await _caseMessageRepository.GetUnreadCountAsync(query.ReturnRequestId, recipientRole.Value, cancellationToken);

        var messageDtos = messages.Select(m => new CaseMessageDto(
            m.Id,
            m.ReturnRequestId,
            m.SenderId,
            m.SenderRole.ToString(),
            m.SenderName,
            m.Content,
            m.SentAt,
            m.IsRead,
            m.ReadAt)).ToList();

        return new CaseMessageThreadDto(
            returnRequest.Id,
            returnRequest.CaseNumber,
            messageDtos.AsReadOnly(),
            unreadCount);
    }

    /// <summary>
    /// Sends a new message in a case thread.
    /// </summary>
    public async Task<SendCaseMessageResultDto> HandleAsync(
        SendCaseMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate content
        if (string.IsNullOrWhiteSpace(command.Content))
        {
            return new SendCaseMessageResultDto(false, "Message content is required.");
        }

        if (command.Content.Length > 5000)
        {
            return new SendCaseMessageResultDto(false, "Message content cannot exceed 5000 characters.");
        }

        // Get the return request
        var returnRequest = await _returnRequestRepository.GetByIdAsync(command.ReturnRequestId, cancellationToken);
        if (returnRequest is null)
        {
            return new SendCaseMessageResultDto(false, "Case not found.");
        }

        // Check authorization
        if (!await IsAuthorizedAsync(returnRequest, command.SenderId, command.SenderRole, cancellationToken))
        {
            return new SendCaseMessageResultDto(false, "You are not authorized to send messages in this case.");
        }

        // Parse sender role
        var senderRole = ParseSenderRole(command.SenderRole);
        if (!senderRole.HasValue)
        {
            return new SendCaseMessageResultDto(false, "Invalid sender role.");
        }

        // Get sender name
        var senderName = await GetSenderNameAsync(command.SenderId, senderRole.Value, returnRequest.StoreId, cancellationToken);
        if (string.IsNullOrEmpty(senderName))
        {
            return new SendCaseMessageResultDto(false, "Sender not found.");
        }

        // Create the message
        var message = new CaseMessage(
            command.ReturnRequestId,
            command.SenderId,
            senderRole.Value,
            senderName,
            command.Content);

        await _caseMessageRepository.AddAsync(message, cancellationToken);
        await _caseMessageRepository.SaveChangesAsync(cancellationToken);

        // Send notification to the other party
        await NotifyRecipientAsync(returnRequest, senderRole.Value, senderName, cancellationToken);

        return new SendCaseMessageResultDto(true, null, message.Id);
    }

    /// <summary>
    /// Marks all messages in a case as read for a specific user.
    /// </summary>
    public async Task HandleAsync(
        MarkCaseMessagesReadCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the return request
        var returnRequest = await _returnRequestRepository.GetByIdAsync(command.ReturnRequestId, cancellationToken);
        if (returnRequest is null)
        {
            return;
        }

        // Check authorization
        if (!await IsAuthorizedAsync(returnRequest, command.UserId, command.UserRole, cancellationToken))
        {
            return;
        }

        // Parse recipient role
        var recipientRole = ParseSenderRole(command.UserRole);
        if (!recipientRole.HasValue)
        {
            return;
        }

        // Mark messages as read
        await _caseMessageRepository.MarkAsReadAsync(command.ReturnRequestId, recipientRole.Value, cancellationToken);
        await _caseMessageRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets unread message count for a buyer.
    /// </summary>
    public async Task<int> HandleAsync(
        GetBuyerUnreadMessageCountQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _caseMessageRepository.GetUnreadCountForBuyerAsync(query.BuyerId, cancellationToken);
    }

    /// <summary>
    /// Gets unread message count for a store.
    /// </summary>
    public async Task<int> HandleAsync(
        GetStoreUnreadMessageCountQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _caseMessageRepository.GetUnreadCountForStoreAsync(query.StoreId, cancellationToken);
    }

    private static CaseMessageSenderRole? ParseSenderRole(string role)
    {
        return role?.ToLowerInvariant() switch
        {
            "buyer" => CaseMessageSenderRole.Buyer,
            "seller" => CaseMessageSenderRole.Seller,
            "admin" => CaseMessageSenderRole.Admin,
            _ => null
        };
    }

    private async Task<bool> IsAuthorizedAsync(
        ReturnRequest returnRequest,
        Guid userId,
        string userRole,
        CancellationToken cancellationToken)
    {
        var role = ParseSenderRole(userRole);
        if (!role.HasValue)
        {
            return false;
        }

        return role.Value switch
        {
            CaseMessageSenderRole.Buyer => returnRequest.BuyerId == userId,
            CaseMessageSenderRole.Seller => await IsSellerForStoreAsync(returnRequest.StoreId, userId, cancellationToken),
            CaseMessageSenderRole.Admin => true, // Admins can access all cases
            _ => false
        };
    }

    private async Task<bool> IsSellerForStoreAsync(Guid storeId, Guid userId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
        return store is not null && store.SellerId == userId;
    }

    private async Task<string?> GetSenderNameAsync(
        Guid senderId,
        CaseMessageSenderRole senderRole,
        Guid storeId,
        CancellationToken cancellationToken)
    {
        if (senderRole == CaseMessageSenderRole.Seller)
        {
            // Use store name for sellers
            var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
            return store?.Name;
        }

        // For buyers and admins, use user name
        var user = await _userRepository.GetByIdAsync(senderId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (senderRole == CaseMessageSenderRole.Admin)
        {
            return "Admin Support";
        }

        // For buyers, use first name or email
        return user.FirstName ?? user.Email?.Value ?? "Buyer";
    }

    private async Task NotifyRecipientAsync(
        ReturnRequest returnRequest,
        CaseMessageSenderRole senderRole,
        string senderName,
        CancellationToken cancellationToken)
    {
        if (senderRole == CaseMessageSenderRole.Buyer)
        {
            // Notify seller
            var store = await _storeRepository.GetByIdAsync(returnRequest.StoreId, cancellationToken);
            if (store is not null)
            {
                var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
                if (seller?.Email is not null)
                {
                    await _notificationService.SendCaseMessageReceivedAsync(
                        returnRequest.Id,
                        returnRequest.CaseNumber,
                        seller.Email.Value,
                        senderName,
                        cancellationToken);
                }
            }
        }
        else if (senderRole == CaseMessageSenderRole.Seller || senderRole == CaseMessageSenderRole.Admin)
        {
            // Notify buyer
            var buyer = await _userRepository.GetByIdAsync(returnRequest.BuyerId, cancellationToken);
            if (buyer?.Email is not null)
            {
                await _notificationService.SendCaseMessageReceivedAsync(
                    returnRequest.Id,
                    returnRequest.CaseNumber,
                    buyer.Email.Value,
                    senderName,
                    cancellationToken);
            }
        }
    }
}
