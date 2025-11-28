using Microsoft.Extensions.DependencyInjection;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Infrastructure;

/// <summary>
/// Provides development seed data for the application.
/// </summary>
public static class SeedDataExtensions
{
    /// <summary>
    /// Development admin user ID.
    /// </summary>
    private static readonly Guid DevAdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Development seller user ID.
    /// </summary>
    private static readonly Guid DevSellerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Development pending seller user ID.
    /// </summary>
    private static readonly Guid DevPendingSellerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>
    /// Seeds development data for testing the public store page functionality.
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var storeRepo = scope.ServiceProvider.GetRequiredService<IStoreRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Create an admin user for development
        var existingAdmin = await userRepo.GetByIdAsync(DevAdminId);
        if (existingAdmin is null)
        {
            var adminEmail = Email.Create("admin@demo.com");
            var passwordHash = passwordHasher.HashPassword("Admin123!");
            var admin = new User(
                DevAdminId,
                adminEmail,
                passwordHash,
                UserRole.Admin,
                "Platform",
                "Admin",
                acceptedTerms: true);
            admin.VerifyEmail();
            await userRepo.AddAsync(admin);
            await userRepo.SaveChangesAsync();
        }

        // Create a test seller user for development
        var existingSeller = await userRepo.GetByIdAsync(DevSellerId);
        if (existingSeller is null)
        {
            var sellerEmail = Email.Create("seller@demo.com");
            var passwordHash = passwordHasher.HashPassword("Demo123!");
            var seller = new User(
                DevSellerId,
                sellerEmail,
                passwordHash,
                UserRole.Seller,
                "Demo",
                "Seller",
                acceptedTerms: true);
            seller.VerifyEmail();
            await userRepo.AddAsync(seller);
            await userRepo.SaveChangesAsync();
        }

        // Create an active store
        var existingStore = await storeRepo.GetBySlugAsync("demo-store");
        if (existingStore is null)
        {
            var store = new Store(DevSellerId, "Demo Store", "contact@demostore.com");
            store.UpdateDescription("Welcome to our demo store! We offer a wide range of quality products.");
            store.UpdateLogoUrl(null);
            store.Activate(); // Make it publicly visible
            await storeRepo.AddAsync(store);
            await storeRepo.SaveChangesAsync();

            // Add some products to the store
            var product1 = new Product(Guid.NewGuid(), store.Id, "Sample Product 1", new Money(29.99m, "USD"), 50, "Electronics");
            product1.Activate();
            var product2 = new Product(Guid.NewGuid(), store.Id, "Sample Product 2", new Money(49.99m, "USD"), 25, "Clothing");
            product2.Activate();
            var product3 = new Product(Guid.NewGuid(), store.Id, "Sample Product 3", new Money(19.99m, "USD"), 100, "Home & Garden");
            product3.Activate();
            await productRepo.AddAsync(product1);
            await productRepo.AddAsync(product2);
            await productRepo.AddAsync(product3);
            await productRepo.SaveChangesAsync();
        }

        // Create a pending verification store
        var pendingStore = await storeRepo.GetBySlugAsync("pending-store");
        if (pendingStore is null)
        {
            var store = new Store(DevPendingSellerId, "Pending Store", "pending@store.com");
            store.UpdateDescription("This store is pending verification.");
            // Don't activate - leave as PendingVerification
            await storeRepo.AddAsync(store);
            await storeRepo.SaveChangesAsync();
        }

        // Create sample categories for the category tree
        var existingCategories = await categoryRepo.GetAllAsync();
        if (existingCategories.Count == 0)
        {
            // Root categories
            var electronicsId = Guid.NewGuid();
            var clothingId = Guid.NewGuid();
            var homeGardenId = Guid.NewGuid();

            var electronics = new Category(electronicsId, "Electronics", null, 1);
            var clothing = new Category(clothingId, "Clothing", null, 2);
            var homeGarden = new Category(homeGardenId, "Home & Garden", null, 3);

            await categoryRepo.AddAsync(electronics);
            await categoryRepo.AddAsync(clothing);
            await categoryRepo.AddAsync(homeGarden);

            // Subcategories for Electronics
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Computers", electronicsId, 1));
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Mobile Phones", electronicsId, 2));
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Audio & Video", electronicsId, 3));

            // Subcategories for Clothing
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Men's Wear", clothingId, 1));
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Women's Wear", clothingId, 2));
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Children's Wear", clothingId, 3));

            // Subcategories for Home & Garden
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Furniture", homeGardenId, 1));
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Kitchen", homeGardenId, 2));
            await categoryRepo.AddAsync(new Category(Guid.NewGuid(), "Garden Tools", homeGardenId, 3));

            await categoryRepo.SaveChangesAsync();
        }
    }
}
