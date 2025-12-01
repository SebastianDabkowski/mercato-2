using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of case message persistence.
/// </summary>
public sealed class CaseMessageRepository : ICaseMessageRepository
{
    private readonly AppDbContext _context;

    public CaseMessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CaseMessage>> GetByReturnRequestIdAsync(Guid returnRequestId, CancellationToken cancellationToken = default)
    {
        var messages = await _context.CaseMessages
            .Where(m => m.ReturnRequestId == returnRequestId)
            .OrderBy(m => m.SentAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return messages.AsReadOnly();
    }

    public async Task<int> GetUnreadCountAsync(Guid returnRequestId, CaseMessageSenderRole recipientRole, CancellationToken cancellationToken = default)
    {
        // Messages are unread for a recipient if:
        // - The message was NOT sent by the recipient role
        // - The message is not marked as read
        return await _context.CaseMessages
            .Where(m => m.ReturnRequestId == returnRequestId)
            .Where(m => m.SenderRole != recipientRole)
            .Where(m => !m.IsRead)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountForBuyerAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        // Get all case IDs for this buyer and count unread messages from sellers/admins
        var query = from message in _context.CaseMessages
                    join request in _context.ReturnRequests on message.ReturnRequestId equals request.Id
                    where request.BuyerId == buyerId
                          && message.SenderRole != CaseMessageSenderRole.Buyer
                          && !message.IsRead
                    select message;

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        // Get all case IDs for this store and count unread messages from buyers
        var query = from message in _context.CaseMessages
                    join request in _context.ReturnRequests on message.ReturnRequestId equals request.Id
                    where request.StoreId == storeId
                          && message.SenderRole == CaseMessageSenderRole.Buyer
                          && !message.IsRead
                    select message;

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(CaseMessage message, CancellationToken cancellationToken = default)
    {
        await _context.CaseMessages.AddAsync(message, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid returnRequestId, CaseMessageSenderRole recipientRole, CancellationToken cancellationToken = default)
    {
        // Mark all unread messages from the other party as read
        var unreadMessages = await _context.CaseMessages
            .Where(m => m.ReturnRequestId == returnRequestId)
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
