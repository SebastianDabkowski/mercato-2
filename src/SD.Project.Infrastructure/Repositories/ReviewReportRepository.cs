using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for review reports.
/// </summary>
public sealed class ReviewReportRepository : IReviewReportRepository
{
    private readonly AppDbContext _context;

    public ReviewReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<ReviewReport?> GetByReviewAndReporterAsync(
        Guid reviewId,
        Guid reporterId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReviewReports
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.ReporterId == reporterId, cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewReport>> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReviewReports
            .AsNoTracking()
            .Where(r => r.ReviewId == reviewId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ReviewReport report, CancellationToken cancellationToken = default)
    {
        await _context.ReviewReports.AddAsync(report, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
