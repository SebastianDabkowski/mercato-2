using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of order message persistence.
/// </summary>
public sealed class OrderMessageRepository : IOrderMessageRepository
{
    private readonly AppDbContext _context;

    public OrderMessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderMessage>> GetMessagesForOrderAsync(
        Guid orderId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var messages = await _context.OrderMessages
            .Where(m => m.OrderId == orderId && m.StoreId == storeId)
            .OrderBy(m => m.SentAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return messages.AsReadOnly();
    }

    public async Task<IReadOnlyList<OrderMessage>> GetLatestMessagesForBuyerAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default)
    {
        // Get the latest message from each order/store thread for this buyer
        var query = from message in _context.OrderMessages
                    join order in _context.Orders on message.OrderId equals order.Id
                    where order.BuyerId == buyerId && !message.IsHidden
                    group message by new { message.OrderId, message.StoreId } into g
                    select g.OrderByDescending(m => m.SentAt).First();

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return messages.AsReadOnly();
    }

    public async Task<IReadOnlyList<OrderMessage>> GetLatestMessagesForStoreAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        // Get the latest message from each order thread for this store
        var query = from message in _context.OrderMessages
                    where message.StoreId == storeId && !message.IsHidden
                    group message by message.OrderId into g
                    select g.OrderByDescending(m => m.SentAt).First();

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return messages.AsReadOnly();
    }

    public async Task<int> GetUnreadCountForBuyerAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        // Count unread messages from sellers/admins for this buyer's orders
        var query = from message in _context.OrderMessages
                    join order in _context.Orders on message.OrderId equals order.Id
                    where order.BuyerId == buyerId
                          && message.SenderRole != OrderMessageSenderRole.Buyer
                          && !message.IsRead
                          && !message.IsHidden
                    select message;

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        // Count unread messages from buyers for this store's order threads
        return await _context.OrderMessages
            .Where(m => m.StoreId == storeId)
            .Where(m => m.SenderRole == OrderMessageSenderRole.Buyer)
            .Where(m => !m.IsRead)
            .Where(m => !m.IsHidden)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrderMessage>> GetHiddenMessagesAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _context.OrderMessages
            .Where(m => m.IsHidden)
            .OrderByDescending(m => m.HiddenAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return messages.AsReadOnly();
    }

    public async Task AddAsync(OrderMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OrderMessages.AddAsync(message, cancellationToken);
    }

    public Task UpdateAsync(OrderMessage message, CancellationToken cancellationToken = default)
    {
        _context.OrderMessages.Update(message);
        return Task.CompletedTask;
    }

    public async Task MarkAsReadAsync(
        Guid orderId,
        Guid storeId,
        OrderMessageSenderRole recipientRole,
        CancellationToken cancellationToken = default)
    {
        // Mark all unread messages from the other party as read
        var unreadMessages = await _context.OrderMessages
            .Where(m => m.OrderId == orderId && m.StoreId == storeId)
            .Where(m => m.SenderRole != recipientRole)
            .Where(m => !m.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var message in unreadMessages)
        {
            message.MarkAsRead();
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
