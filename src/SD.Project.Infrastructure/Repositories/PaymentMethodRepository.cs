using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of payment method persistence.
/// </summary>
public sealed class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly AppDbContext _context;

    public PaymentMethodRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentMethod?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .Where(p => p.IsActive && p.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        await _context.PaymentMethods.AddAsync(paymentMethod, cancellationToken);
    }

    public Task UpdateAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        _context.PaymentMethods.Update(paymentMethod);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
