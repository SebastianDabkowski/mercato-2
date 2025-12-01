using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of product question persistence.
/// </summary>
public sealed class ProductQuestionRepository : IProductQuestionRepository
{
    private readonly AppDbContext _context;

    public ProductQuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductQuestion?> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductQuestion>> GetPublicQuestionsForProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var questions = await _context.ProductQuestions
            .Where(q => q.ProductId == productId)
            .Where(q => q.Status == ProductQuestionStatus.Answered)
            .OrderByDescending(q => q.AnsweredAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return questions.AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductQuestion>> GetAllQuestionsForProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var questions = await _context.ProductQuestions
            .Where(q => q.ProductId == productId)
            .OrderByDescending(q => q.AskedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return questions.AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductQuestion>> GetPendingQuestionsForStoreAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var questions = await _context.ProductQuestions
            .Where(q => q.StoreId == storeId)
            .Where(q => q.Status == ProductQuestionStatus.Pending)
            .OrderBy(q => q.AskedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return questions.AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductQuestion>> GetAllQuestionsForStoreAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var questions = await _context.ProductQuestions
            .Where(q => q.StoreId == storeId)
            .Where(q => q.Status != ProductQuestionStatus.Hidden)
            .OrderByDescending(q => q.AskedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return questions.AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductQuestion>> GetQuestionsByBuyerAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default)
    {
        var questions = await _context.ProductQuestions
            .Where(q => q.BuyerId == buyerId)
            .OrderByDescending(q => q.AskedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return questions.AsReadOnly();
    }

    public async Task<int> GetUnansweredCountForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductQuestions
            .Where(q => q.StoreId == storeId)
            .Where(q => q.Status == ProductQuestionStatus.Pending)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductQuestion>> GetHiddenQuestionsAsync(CancellationToken cancellationToken = default)
    {
        var questions = await _context.ProductQuestions
            .Where(q => q.Status == ProductQuestionStatus.Hidden)
            .OrderByDescending(q => q.HiddenAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return questions.AsReadOnly();
    }

    public async Task AddAsync(ProductQuestion question, CancellationToken cancellationToken = default)
    {
        await _context.ProductQuestions.AddAsync(question, cancellationToken);
    }

    public Task UpdateAsync(ProductQuestion question, CancellationToken cancellationToken = default)
    {
        _context.ProductQuestions.Update(question);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
