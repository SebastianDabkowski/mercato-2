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
    /// Seeds development data for testing the public store page functionality.
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var storeRepo = scope.ServiceProvider.GetRequiredService<IStoreRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Create a test seller user ID
        var sellerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Create a test seller user for development
        var existingSeller = await userRepo.GetByIdAsync(sellerId);
        if (existingSeller is null)
        {
            var sellerEmail = Email.Create("seller@demo.com");
            var passwordHash = passwordHasher.HashPassword("Demo123!");
            var seller = new User(
                sellerId,
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
            var store = new Store(sellerId, "Demo Store", "contact@demostore.com");
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
        var pendingSellerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var pendingStore = await storeRepo.GetBySlugAsync("pending-store");
        if (pendingStore is null)
        {
            var store = new Store(pendingSellerId, "Pending Store", "pending@store.com");
            store.UpdateDescription("This store is pending verification.");
            // Don't activate - leave as PendingVerification
            await storeRepo.AddAsync(store);
            await storeRepo.SaveChangesAsync();
        }
    }
}
