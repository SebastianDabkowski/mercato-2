using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed seller onboarding repository.
/// </summary>
public sealed class SellerOnboardingRepository : ISellerOnboardingRepository
{
    private readonly AppDbContext _context;

    public SellerOnboardingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SellerOnboarding?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.SellerOnboardings.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<SellerOnboarding?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SellerOnboardings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(SellerOnboarding onboarding, CancellationToken cancellationToken = default)
    {
        await _context.SellerOnboardings.AddAsync(onboarding, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
