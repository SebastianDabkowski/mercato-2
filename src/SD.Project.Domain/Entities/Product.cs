namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a catalog product aggregate root.
/// </summary>
public class Product
{
    public Guid Id { get; private set; }
    public Guid? StoreId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = string.Empty;
    public ValueObjects.Money Price { get; private set; } = default!;
    public int Stock { get; private set; }
    public string Category { get; private set; } = default!;
    public ProductStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Shipping parameters
    public decimal? WeightKg { get; private set; }
    public decimal? LengthCm { get; private set; }
    public decimal? WidthCm { get; private set; }
    public decimal? HeightCm { get; private set; }

    private Product()
    {
        // EF Core constructor
    }

    public Product(Guid id, string name, ValueObjects.Money price, Guid? storeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required", nameof(name));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StoreId = storeId;
        Name = name;
        Price = price;
        Stock = 0;
        Category = string.Empty;
        Status = ProductStatus.Draft;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Product(Guid id, Guid storeId, string name, ValueObjects.Money price, int stock, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required", nameof(name));
        }

        if (stock < 0)
        {
            throw new ArgumentException("Stock cannot be negative", nameof(stock));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required", nameof(category));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StoreId = storeId;
        Name = name;
        Price = price;
        Stock = stock;
        Category = category;
        Status = ProductStatus.Draft;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required", nameof(name));
        }

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int stock)
    {
        if (stock < 0)
        {
            throw new ArgumentException("Stock cannot be negative", nameof(stock));
        }

        Stock = stock;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required", nameof(category));
        }

        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = ProductStatus.Active;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(ValueObjects.Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateShippingParameters(decimal? weightKg, decimal? lengthCm, decimal? widthCm, decimal? heightCm)
    {
        if (weightKg.HasValue && weightKg.Value < 0)
        {
            throw new ArgumentException("Weight cannot be negative", nameof(weightKg));
        }

        if (lengthCm.HasValue && lengthCm.Value < 0)
        {
            throw new ArgumentException("Length cannot be negative", nameof(lengthCm));
        }

        if (widthCm.HasValue && widthCm.Value < 0)
        {
            throw new ArgumentException("Width cannot be negative", nameof(widthCm));
        }

        if (heightCm.HasValue && heightCm.Value < 0)
        {
            throw new ArgumentException("Height cannot be negative", nameof(heightCm));
        }

        WeightKg = weightKg;
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Archives the product (soft-delete). Archived products are not visible in search or public listings.
    /// </summary>
    public void Archive()
    {
        Status = ProductStatus.Archived;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the product is archived.
    /// </summary>
    public bool IsArchived => Status == ProductStatus.Archived;
}
