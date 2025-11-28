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

    /// <summary>
    /// Stock Keeping Unit - unique identifier for merchant's inventory management.
    /// Used as matching key for bulk imports.
    /// </summary>
    public string? Sku { get; private set; }

    // Shipping parameters
    public decimal? WeightKg { get; private set; }
    public decimal? LengthCm { get; private set; }
    public decimal? WidthCm { get; private set; }
    public decimal? HeightCm { get; private set; }

    /// <summary>
    /// Indicates whether this product has variants enabled.
    /// When true, stock and price are managed at the variant level.
    /// </summary>
    public bool HasVariants { get; private set; }

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

    /// <summary>
    /// Checks if the product can be transitioned to Active status.
    /// Active products must meet minimum data quality rules.
    /// </summary>
    /// <returns>A list of validation errors. Empty if transition is allowed.</returns>
    public IReadOnlyList<string> ValidateForActivation()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Description))
        {
            errors.Add("Description is required to activate a product.");
        }

        if (string.IsNullOrWhiteSpace(Category))
        {
            errors.Add("Category is required to activate a product.");
        }

        if (Price is null || Price.Amount <= 0)
        {
            errors.Add("A valid price greater than zero is required to activate a product.");
        }

        if (Stock < 0)
        {
            errors.Add("Stock cannot be negative to activate a product.");
        }

        return errors;
    }

    /// <summary>
    /// Checks if a status transition is allowed based on workflow rules.
    /// </summary>
    /// <param name="targetStatus">The desired target status.</param>
    /// <param name="isAdminOverride">Whether the transition is an admin override.</param>
    /// <returns>True if the transition is allowed; otherwise, false.</returns>
    public bool CanTransitionTo(ProductStatus targetStatus, bool isAdminOverride = false)
    {
        // Admin can override most transitions except from Archived
        if (isAdminOverride && Status != ProductStatus.Archived)
        {
            return true;
        }

        // No transition allowed from Archived (must remain for audit)
        if (Status == ProductStatus.Archived)
        {
            return false;
        }

        // Same status is allowed (no-op)
        if (Status == targetStatus)
        {
            return true;
        }

        return Status switch
        {
            ProductStatus.Draft => targetStatus is ProductStatus.Active or ProductStatus.Archived,
            ProductStatus.Active => targetStatus is ProductStatus.Suspended or ProductStatus.Archived,
            ProductStatus.Inactive => targetStatus is ProductStatus.Active or ProductStatus.Suspended or ProductStatus.Archived,
            ProductStatus.Suspended => targetStatus is ProductStatus.Active or ProductStatus.Archived,
            _ => false
        };
    }

    /// <summary>
    /// Transitions the product to a new status with workflow validation.
    /// </summary>
    /// <param name="targetStatus">The desired target status.</param>
    /// <param name="isAdminOverride">Whether this is an admin override.</param>
    /// <returns>A list of validation errors. Empty if transition succeeded.</returns>
    public IReadOnlyList<string> TransitionTo(ProductStatus targetStatus, bool isAdminOverride = false)
    {
        var errors = new List<string>();

        if (!CanTransitionTo(targetStatus, isAdminOverride))
        {
            errors.Add($"Cannot transition from {Status} to {targetStatus}.");
            return errors;
        }

        // If transitioning to Active, validate data quality requirements
        if (targetStatus == ProductStatus.Active)
        {
            var activationErrors = ValidateForActivation();
            if (activationErrors.Count > 0)
            {
                return activationErrors;
            }
        }

        // Apply the transition
        Status = targetStatus;
        IsActive = targetStatus == ProductStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        return errors;
    }

    /// <summary>
    /// Activates the product. Use TransitionTo for workflow-aware activation with validation.
    /// </summary>
    [Obsolete("Use TransitionTo(ProductStatus.Active) for workflow-aware activation. This method will be removed in a future version. Migrate by replacing Activate() calls with TransitionTo(ProductStatus.Active).")]
    public void Activate()
    {
        Status = ProductStatus.Active;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the product by setting IsActive to false.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspends the product. It is no longer available for new orders but may remain visible in order history.
    /// </summary>
    /// <returns>A list of validation errors. Empty if transition succeeded.</returns>
    public IReadOnlyList<string> Suspend()
    {
        return TransitionTo(ProductStatus.Suspended);
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
    /// <returns>A list of validation errors. Empty if transition succeeded.</returns>
    public IReadOnlyList<string> Archive()
    {
        return TransitionTo(ProductStatus.Archived);
    }

    /// <summary>
    /// Checks if the product is archived.
    /// </summary>
    public bool IsArchived => Status == ProductStatus.Archived;

    /// <summary>
    /// Checks if the product is suspended.
    /// </summary>
    public bool IsSuspended => Status == ProductStatus.Suspended;

    /// <summary>
    /// Updates the product's SKU (Stock Keeping Unit).
    /// </summary>
    /// <param name="sku">The new SKU value. Can be null to clear the SKU.</param>
    public void UpdateSku(string? sku)
    {
        Sku = sku?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables variants for this product.
    /// When variants are enabled, stock and price are managed at the variant level.
    /// </summary>
    public void EnableVariants()
    {
        HasVariants = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables variants for this product.
    /// Stock and price will be managed at the product level.
    /// </summary>
    public void DisableVariants()
    {
        HasVariants = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
