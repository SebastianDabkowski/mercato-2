using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for order message operations.
/// Handles private messaging between buyers and sellers about orders.
/// </summary>
public sealed class OrderMessageService
{
    private readonly IOrderMessageRepository _messageRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public OrderMessageService(
        IOrderMessageRepository messageRepository,
        IOrderRepository orderRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _messageRepository = messageRepository;
        _orderRepository = orderRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets a message thread for an order and store.
    /// </summary>
    public async Task<OrderMessageThreadDto?> HandleAsync(
        GetOrderMessageThreadQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Get the order
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        // Get the store
        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
        {
            return null;
        }

        // Verify authorization
        if (!await IsAuthorizedAsync(order, store, query.UserId, query.UserRole, cancellationToken))
        {
            return null;
        }

        // Get messages
        var messages = await _messageRepository.GetMessagesForOrderAsync(query.OrderId, query.StoreId, cancellationToken);

        // Filter out hidden messages unless user is admin
        var visibleMessages = ParseSenderRole(query.UserRole) == OrderMessageSenderRole.Admin
            ? messages
            : messages.Where(m => !m.IsHidden).ToList().AsReadOnly();

        // Calculate unread count based on user role
        var recipientRole = ParseSenderRole(query.UserRole);
        if (!recipientRole.HasValue)
        {
            return null;
        }

        var unreadCount = visibleMessages.Count(m => !m.IsRead && m.SenderRole != recipientRole.Value);

        var messageDtos = visibleMessages.Select(m => new OrderMessageDto(
            m.Id,
            m.OrderId,
            m.StoreId,
            m.SenderId,
            m.SenderRole.ToString(),
            m.SenderName,
            m.Content,
            m.SentAt,
            m.IsRead,
            m.ReadAt)).ToList();

        return new OrderMessageThreadDto(
            order.Id,
            order.OrderNumber,
            store.Id,
            store.Name,
            messageDtos.AsReadOnly(),
            unreadCount);
    }

    /// <summary>
    /// Gets all message threads for a buyer.
    /// </summary>
    public async Task<IReadOnlyList<OrderMessageThreadSummaryDto>> HandleAsync(
        GetBuyerMessageThreadsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var latestMessages = await _messageRepository.GetLatestMessagesForBuyerAsync(query.BuyerId, cancellationToken);

        var summaries = new List<OrderMessageThreadSummaryDto>();
        foreach (var message in latestMessages)
        {
            var order = await _orderRepository.GetByIdAsync(message.OrderId, cancellationToken);
            var store = await _storeRepository.GetByIdAsync(message.StoreId, cancellationToken);

            if (order is not null && store is not null)
            {
                // Get all messages in thread to count unread
                var threadMessages = await _messageRepository.GetMessagesForOrderAsync(message.OrderId, message.StoreId, cancellationToken);
                var unreadCount = threadMessages.Count(m => !m.IsRead && m.SenderRole != OrderMessageSenderRole.Buyer);

                summaries.Add(new OrderMessageThreadSummaryDto(
                    order.Id,
                    order.OrderNumber,
                    store.Id,
                    store.Name,
                    TruncateContent(message.Content),
                    message.SentAt,
                    unreadCount));
            }
        }

        return summaries.AsReadOnly();
    }

    /// <summary>
    /// Gets all message threads for a store.
    /// </summary>
    public async Task<IReadOnlyList<OrderMessageThreadSummaryDto>?> HandleAsync(
        GetStoreMessageThreadsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Verify seller owns the store
        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null || store.SellerId != query.SellerId)
        {
            return null;
        }

        var latestMessages = await _messageRepository.GetLatestMessagesForStoreAsync(query.StoreId, cancellationToken);

        var summaries = new List<OrderMessageThreadSummaryDto>();
        foreach (var message in latestMessages)
        {
            var order = await _orderRepository.GetByIdAsync(message.OrderId, cancellationToken);

            if (order is not null)
            {
                // Get all messages in thread to count unread
                var threadMessages = await _messageRepository.GetMessagesForOrderAsync(message.OrderId, message.StoreId, cancellationToken);
                var unreadCount = threadMessages.Count(m => !m.IsRead && m.SenderRole != OrderMessageSenderRole.Seller);

                summaries.Add(new OrderMessageThreadSummaryDto(
                    order.Id,
                    order.OrderNumber,
                    store.Id,
                    store.Name,
                    TruncateContent(message.Content),
                    message.SentAt,
                    unreadCount));
            }
        }

        return summaries.AsReadOnly();
    }

    /// <summary>
    /// Gets unread message count for a buyer.
    /// </summary>
    public async Task<int> HandleAsync(
        GetBuyerUnreadOrderMessageCountQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _messageRepository.GetUnreadCountForBuyerAsync(query.BuyerId, cancellationToken);
    }

    /// <summary>
    /// Gets unread message count for a store.
    /// </summary>
    public async Task<int> HandleAsync(
        GetStoreUnreadOrderMessageCountQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _messageRepository.GetUnreadCountForStoreAsync(query.StoreId, cancellationToken);
    }

    /// <summary>
    /// Gets hidden messages (admin moderation view).
    /// </summary>
    public async Task<IReadOnlyList<OrderMessageDto>> HandleAsync(
        GetHiddenOrderMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var messages = await _messageRepository.GetHiddenMessagesAsync(cancellationToken);

        return messages.Select(m => new OrderMessageDto(
            m.Id,
            m.OrderId,
            m.StoreId,
            m.SenderId,
            m.SenderRole.ToString(),
            m.SenderName,
            m.Content,
            m.SentAt,
            m.IsRead,
            m.ReadAt)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Sends a new message in an order thread.
    /// </summary>
    public async Task<SendOrderMessageResultDto> HandleAsync(
        SendOrderMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate content
        if (string.IsNullOrWhiteSpace(command.Content))
        {
            return new SendOrderMessageResultDto(false, "Message content is required.");
        }

        if (command.Content.Length > 5000)
        {
            return new SendOrderMessageResultDto(false, "Message content cannot exceed 5000 characters.");
        }

        // Get the order
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return new SendOrderMessageResultDto(false, "Order not found.");
        }

        // Get the store
        var store = await _storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
        {
            return new SendOrderMessageResultDto(false, "Store not found.");
        }

        // Verify the store is part of this order
        if (!order.Items.Any(i => i.StoreId == command.StoreId))
        {
            return new SendOrderMessageResultDto(false, "Store is not part of this order.");
        }

        // Verify authorization
        if (!await IsAuthorizedAsync(order, store, command.SenderId, command.SenderRole, cancellationToken))
        {
            return new SendOrderMessageResultDto(false, "You are not authorized to send messages in this thread.");
        }

        // Parse sender role
        var senderRole = ParseSenderRole(command.SenderRole);
        if (!senderRole.HasValue)
        {
            return new SendOrderMessageResultDto(false, "Invalid sender role.");
        }

        // Get sender name
        var senderName = await GetSenderNameAsync(command.SenderId, senderRole.Value, store.Id, cancellationToken);
        if (string.IsNullOrEmpty(senderName))
        {
            return new SendOrderMessageResultDto(false, "Sender not found.");
        }

        // Create the message
        var message = new OrderMessage(
            command.OrderId,
            command.StoreId,
            command.SenderId,
            senderRole.Value,
            senderName,
            command.Content);

        await _messageRepository.AddAsync(message, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        // Send notification to the other party
        await NotifyRecipientAsync(order, store, senderRole.Value, senderName, cancellationToken);

        return new SendOrderMessageResultDto(true, null, message.Id);
    }

    /// <summary>
    /// Marks messages as read in an order thread.
    /// </summary>
    public async Task HandleAsync(
        MarkOrderMessagesReadCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the order
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        // Get the store
        var store = await _storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
        {
            return;
        }

        // Verify authorization
        if (!await IsAuthorizedAsync(order, store, command.UserId, command.UserRole, cancellationToken))
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
        await _messageRepository.MarkAsReadAsync(command.OrderId, command.StoreId, recipientRole.Value, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Hides an order message (admin moderation).
    /// </summary>
    public async Task<ModerateOrderMessageResultDto> HandleAsync(
        HideOrderMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new ModerateOrderMessageResultDto(false, "Reason is required.");
        }

        var message = await _messageRepository.GetByIdAsync(command.MessageId, cancellationToken);
        if (message is null)
        {
            return new ModerateOrderMessageResultDto(false, "Message not found.");
        }

        try
        {
            message.Hide(command.AdminId, command.Reason);
        }
        catch (ArgumentException ex)
        {
            return new ModerateOrderMessageResultDto(false, ex.Message);
        }

        await _messageRepository.UpdateAsync(message, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        return new ModerateOrderMessageResultDto(true, null);
    }

    /// <summary>
    /// Unhides an order message (admin moderation).
    /// </summary>
    public async Task<ModerateOrderMessageResultDto> HandleAsync(
        UnhideOrderMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var message = await _messageRepository.GetByIdAsync(command.MessageId, cancellationToken);
        if (message is null)
        {
            return new ModerateOrderMessageResultDto(false, "Message not found.");
        }

        try
        {
            message.Unhide();
        }
        catch (InvalidOperationException ex)
        {
            return new ModerateOrderMessageResultDto(false, ex.Message);
        }

        await _messageRepository.UpdateAsync(message, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        return new ModerateOrderMessageResultDto(true, null);
    }

    private static OrderMessageSenderRole? ParseSenderRole(string role)
    {
        return role?.ToLowerInvariant() switch
        {
            "buyer" => OrderMessageSenderRole.Buyer,
            "seller" => OrderMessageSenderRole.Seller,
            "admin" => OrderMessageSenderRole.Admin,
            _ => null
        };
    }

    private async Task<bool> IsAuthorizedAsync(
        Order order,
        Store store,
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
            OrderMessageSenderRole.Buyer => order.BuyerId == userId,
            OrderMessageSenderRole.Seller => store.SellerId == userId,
            OrderMessageSenderRole.Admin => true, // Admins can access all threads
            _ => false
        };
    }

    private async Task<string?> GetSenderNameAsync(
        Guid senderId,
        OrderMessageSenderRole senderRole,
        Guid storeId,
        CancellationToken cancellationToken)
    {
        if (senderRole == OrderMessageSenderRole.Seller)
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

        if (senderRole == OrderMessageSenderRole.Admin)
        {
            return "Admin Support";
        }

        // For buyers, use first name or email
        return user.FirstName ?? user.Email?.Value ?? "Buyer";
    }

    private async Task NotifyRecipientAsync(
        Order order,
        Store store,
        OrderMessageSenderRole senderRole,
        string senderName,
        CancellationToken cancellationToken)
    {
        if (senderRole == OrderMessageSenderRole.Buyer)
        {
            // Notify seller
            var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
            if (seller?.Email is not null)
            {
                await _notificationService.SendOrderMessageReceivedAsync(
                    order.Id,
                    order.OrderNumber,
                    seller.Email.Value,
                    senderName,
                    cancellationToken);
            }
        }
        else if (senderRole == OrderMessageSenderRole.Seller || senderRole == OrderMessageSenderRole.Admin)
        {
            // Notify buyer
            var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
            if (buyer?.Email is not null)
            {
                await _notificationService.SendOrderMessageReceivedAsync(
                    order.Id,
                    order.OrderNumber,
                    buyer.Email.Value,
                    senderName,
                    cancellationToken);
            }
        }
    }

    private static string TruncateContent(string content, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
        {
            return content;
        }
        return content[..(maxLength - 3)] + "...";
    }
}
