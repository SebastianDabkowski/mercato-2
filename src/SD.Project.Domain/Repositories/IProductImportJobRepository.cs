using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for product import job persistence operations.
/// </summary>
public interface IProductImportJobRepository
{
    Task<ProductImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductImportJob>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductImportJob job, CancellationToken cancellationToken = default);
    void Update(ProductImportJob job);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
