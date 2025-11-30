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
        var paymentMethodRepo = scope.ServiceProvider.GetRequiredService<IPaymentMethodRepository>();
        var shippingMethodRepo = scope.ServiceProvider.GetRequiredService<IShippingMethodRepository>();
        var promoCodeRepo = scope.ServiceProvider.GetRequiredService<IPromoCodeRepository>();

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

        // Create a test buyer user for development
        var devBuyerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var existingBuyer = await userRepo.GetByIdAsync(devBuyerId);
        if (existingBuyer is null)
        {
            var buyerEmail = Email.Create("buyer@demo.com");
            var passwordHash = passwordHasher.HashPassword("Buyer123!");
            var buyer = new User(
                devBuyerId,
                buyerEmail,
                passwordHash,
                UserRole.Buyer,
                "Demo",
                "Buyer",
                acceptedTerms: true);
            buyer.VerifyEmail();
            await userRepo.AddAsync(buyer);
            await userRepo.SaveChangesAsync();
        }

        // Create an active store
        var existingStore = await storeRepo.GetBySlugAsync("demo-store");
        Store? demoStore = existingStore;
        if (existingStore is null)
        {
            var store = new Store(DevSellerId, "Demo Store", "contact@demostore.com");
            store.UpdateDescription("Welcome to our demo store! We offer a wide range of quality products.");
            store.UpdateLogoUrl(null);
            store.Activate(); // Make it publicly visible
            await storeRepo.AddAsync(store);
            await storeRepo.SaveChangesAsync();
            demoStore = store;

            // Add some products to the store
            var product1 = new Product(Guid.NewGuid(), store.Id, "Sample Product 1", new Money(29.99m, "USD"), 50, "Electronics");
            product1.UpdateDescription("A sample electronics product for testing.");
            product1.TransitionTo(ProductStatus.Active);
            var product2 = new Product(Guid.NewGuid(), store.Id, "Sample Product 2", new Money(49.99m, "USD"), 25, "Clothing");
            product2.UpdateDescription("A sample clothing product for testing.");
            product2.TransitionTo(ProductStatus.Active);
            var product3 = new Product(Guid.NewGuid(), store.Id, "Sample Product 3", new Money(19.99m, "USD"), 100, "Home & Garden");
            product3.UpdateDescription("A sample home & garden product for testing.");
            product3.TransitionTo(ProductStatus.Active);
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

        // Seed payment methods
        var existingPaymentMethods = await paymentMethodRepo.GetAllAsync();
        if (existingPaymentMethods.Count == 0)
        {
            var creditCard = new PaymentMethod(
                "Credit Card",
                PaymentMethodType.Card,
                "stripe",
                "Pay securely with Visa, Mastercard, or American Express",
                iconClass: null,
                displayOrder: 1,
                feePercentage: 2.9m,
                feeFixed: 0.30m,
                isDefault: true);

            var paypal = new PaymentMethod(
                "PayPal",
                PaymentMethodType.DigitalWallet,
                "paypal",
                "Pay with your PayPal account",
                iconClass: null,
                displayOrder: 2,
                feePercentage: 3.49m,
                feeFixed: 0.49m,
                isDefault: false);

            var bankTransfer = new PaymentMethod(
                "Bank Transfer",
                PaymentMethodType.BankTransfer,
                "bank",
                "Direct bank transfer - order ships after payment clears",
                iconClass: null,
                displayOrder: 3,
                feePercentage: null,
                feeFixed: null,
                isDefault: false);

            await paymentMethodRepo.AddAsync(creditCard);
            await paymentMethodRepo.AddAsync(paypal);
            await paymentMethodRepo.AddAsync(bankTransfer);
            await paymentMethodRepo.SaveChangesAsync();
        }

        // Seed shipping methods (platform-wide defaults)
        var existingShippingMethods = await shippingMethodRepo.GetPlatformMethodsAsync();
        if (existingShippingMethods.Count == 0)
        {
            var standardShipping = new ShippingMethod(
                storeId: null, // Platform-wide
                name: "Standard Shipping",
                description: "Delivered by postal service",
                carrierName: "USPS",
                estimatedDeliveryDaysMin: 5,
                estimatedDeliveryDaysMax: 7,
                baseCost: 4.99m,
                costPerItem: 0.50m,
                currency: "USD",
                freeShippingThreshold: 50.00m,
                displayOrder: 1,
                isDefault: true);

            var expressShipping = new ShippingMethod(
                storeId: null, // Platform-wide
                name: "Express Shipping",
                description: "Fast delivery via courier",
                carrierName: "FedEx",
                estimatedDeliveryDaysMin: 2,
                estimatedDeliveryDaysMax: 3,
                baseCost: 9.99m,
                costPerItem: 1.00m,
                currency: "USD",
                freeShippingThreshold: 100.00m,
                displayOrder: 2,
                isDefault: false);

            var overnightShipping = new ShippingMethod(
                storeId: null, // Platform-wide
                name: "Overnight Shipping",
                description: "Next business day delivery",
                carrierName: "UPS",
                estimatedDeliveryDaysMin: 1,
                estimatedDeliveryDaysMax: 1,
                baseCost: 19.99m,
                costPerItem: 2.00m,
                currency: "USD",
                freeShippingThreshold: null,
                displayOrder: 3,
                isDefault: false);

            await shippingMethodRepo.AddAsync(standardShipping);
            await shippingMethodRepo.AddAsync(expressShipping);
            await shippingMethodRepo.AddAsync(overnightShipping);
            await shippingMethodRepo.SaveChangesAsync();
        }

        // Seed promo codes for testing
        var existingPromoCode = await promoCodeRepo.GetByCodeAsync("WELCOME10");
        if (existingPromoCode is null)
        {
            // Platform-wide 10% off promo code
            var welcome10 = PromoCode.CreatePlatformPromo(
                code: "WELCOME10",
                description: "10% off for new customers",
                discountType: PromoDiscountType.Percentage,
                discountValue: 10m,
                currency: "USD",
                validFrom: DateTime.UtcNow.AddDays(-30),
                validTo: DateTime.UtcNow.AddDays(365),
                minimumOrderAmount: 20m,
                maximumDiscountAmount: 50m,
                maxUsageCount: 1000,
                maxUsagePerUser: 1);

            await promoCodeRepo.AddAsync(welcome10);

            // Platform-wide $5 off promo code
            var save5 = PromoCode.CreatePlatformPromo(
                code: "SAVE5",
                description: "$5 off your order",
                discountType: PromoDiscountType.FixedAmount,
                discountValue: 5m,
                currency: "USD",
                validFrom: DateTime.UtcNow.AddDays(-30),
                validTo: DateTime.UtcNow.AddDays(180),
                minimumOrderAmount: 25m,
                maximumDiscountAmount: null,
                maxUsageCount: null,
                maxUsagePerUser: 3);

            await promoCodeRepo.AddAsync(save5);

            // Expired promo code for testing
            var expired = PromoCode.CreatePlatformPromo(
                code: "EXPIRED20",
                description: "Expired 20% off promo",
                discountType: PromoDiscountType.Percentage,
                discountValue: 20m,
                currency: "USD",
                validFrom: DateTime.UtcNow.AddDays(-60),
                validTo: DateTime.UtcNow.AddDays(-1),
                minimumOrderAmount: null,
                maximumDiscountAmount: null,
                maxUsageCount: null,
                maxUsagePerUser: null);

            await promoCodeRepo.AddAsync(expired);

            await promoCodeRepo.SaveChangesAsync();
        }
    }
}
