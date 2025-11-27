namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a catalog product aggregate root.
/// </summary>
public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public ValueObjects.Money Price { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private Product()
    {
        // EF Core constructor
    }

    public Product(Guid id, string name, ValueObjects.Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required", nameof(name));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name;
        Price = price;
        IsActive = true;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required", nameof(name));
        }

        Name = name;
    }

    public void Deactivate() => IsActive = false;
}
