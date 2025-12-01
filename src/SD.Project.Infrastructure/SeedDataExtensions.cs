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

        // Seed sample orders for testing buyer order detail view
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var addressRepo = scope.ServiceProvider.GetRequiredService<IDeliveryAddressRepository>();

        var buyerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var orderStore = await storeRepo.GetBySlugAsync("demo-store");
        var existingOrders = await orderRepo.GetByBuyerIdAsync(buyerId);

        if (existingOrders.Count == 0 && orderStore is not null)
        {
            // Get or create a delivery address
            var buyerAddresses = await addressRepo.GetByBuyerIdAsync(buyerId);
            DeliveryAddress? deliveryAddress;
            if (buyerAddresses.Count == 0)
            {
                var address = Address.Create("123 Main Street", null, "New York", "NY", "10001", "USA");
                deliveryAddress = new DeliveryAddress(buyerId, "Demo Buyer", address, "+1234567890", "Home", true);
                await addressRepo.AddAsync(deliveryAddress);
                await addressRepo.SaveChangesAsync();
            }
            else
            {
                deliveryAddress = buyerAddresses.First();
            }

            // Get payment methods
            var paymentMethods = await paymentMethodRepo.GetAllAsync();
            var defaultPaymentMethod = paymentMethods.FirstOrDefault(p => p.IsDefault) ?? paymentMethods.First();

            // Get products for order items
            var allProducts = await productRepo.GetAllByStoreIdAsync(orderStore.Id);
            var products = allProducts.Take(10).ToList();

            // Order 1: Delivered order
            var order1 = new Order(
                buyerId,
                "ORD-2024-0001",
                deliveryAddress.Id,
                deliveryAddress.RecipientName,
                deliveryAddress.Street,
                deliveryAddress.Street2,
                deliveryAddress.City,
                deliveryAddress.State,
                deliveryAddress.PostalCode,
                deliveryAddress.Country,
                deliveryAddress.PhoneNumber,
                "Please leave at door",
                defaultPaymentMethod.Id,
                defaultPaymentMethod.Name,
                "USD");

            if (products.Count > 0)
            {
                order1.AddItem(products[0].Id, orderStore.Id, products[0].Name, products[0].Price.Amount, 2, null, "Standard Shipping", 4.99m);
            }
            if (products.Count > 1)
            {
                order1.AddItem(products[1].Id, orderStore.Id, products[1].Name, products[1].Price.Amount, 1, null, "Standard Shipping", 0m);
            }
            order1.CreateShipments();
            order1.ConfirmPayment("TXN-001-DEMO");
            order1.StartProcessing();

            // Ship the order
            foreach (var shipment in order1.Shipments)
            {
                shipment.StartProcessing();
                shipment.Ship("FedEx", "FX123456789", "https://www.fedex.com/apps/fedextrack/?tracknumbers=FX123456789");
                shipment.MarkDelivered();
            }
            order1.MarkShipped();
            order1.MarkDelivered();

            await orderRepo.AddAsync(order1);

            // Order 2: Cancelled order
            var order2 = new Order(
                buyerId,
                "ORD-2024-0002",
                deliveryAddress.Id,
                deliveryAddress.RecipientName,
                deliveryAddress.Street,
                deliveryAddress.Street2,
                deliveryAddress.City,
                deliveryAddress.State,
                deliveryAddress.PostalCode,
                deliveryAddress.Country,
                deliveryAddress.PhoneNumber,
                null,
                defaultPaymentMethod.Id,
                defaultPaymentMethod.Name,
                "USD");

            if (products.Count > 0)
            {
                order2.AddItem(products[0].Id, orderStore.Id, products[0].Name, products[0].Price.Amount, 1, null, "Express Shipping", 9.99m);
            }
            order2.CreateShipments();
            order2.Cancel();

            await orderRepo.AddAsync(order2);

            // Order 3: Refunded order
            var order3 = new Order(
                buyerId,
                "ORD-2024-0003",
                deliveryAddress.Id,
                deliveryAddress.RecipientName,
                deliveryAddress.Street,
                deliveryAddress.Street2,
                deliveryAddress.City,
                deliveryAddress.State,
                deliveryAddress.PostalCode,
                deliveryAddress.Country,
                deliveryAddress.PhoneNumber,
                null,
                defaultPaymentMethod.Id,
                defaultPaymentMethod.Name,
                "USD");

            if (products.Count > 1)
            {
                order3.AddItem(products[1].Id, orderStore.Id, products[1].Name, products[1].Price.Amount, 2, null, "Standard Shipping", 4.99m);
            }
            order3.CreateShipments();
            order3.ConfirmPayment("TXN-003-DEMO");
            order3.StartProcessing();
            order3.Refund();

            await orderRepo.AddAsync(order3);

            // Order 4: In processing
            var order4 = new Order(
                buyerId,
                "ORD-2024-0004",
                deliveryAddress.Id,
                deliveryAddress.RecipientName,
                deliveryAddress.Street,
                deliveryAddress.Street2,
                deliveryAddress.City,
                deliveryAddress.State,
                deliveryAddress.PostalCode,
                deliveryAddress.Country,
                deliveryAddress.PhoneNumber,
                "Ring doorbell twice",
                defaultPaymentMethod.Id,
                defaultPaymentMethod.Name,
                "USD");

            if (products.Count > 2)
            {
                order4.AddItem(products[2].Id, orderStore.Id, products[2].Name, products[2].Price.Amount, 3, null, "Overnight Shipping", 19.99m);
            }
            order4.CreateShipments();
            order4.ConfirmPayment("TXN-004-DEMO");
            order4.StartProcessing();

            await orderRepo.AddAsync(order4);

            // Order 5: Shipped
            var order5 = new Order(
                buyerId,
                "ORD-2024-0005",
                deliveryAddress.Id,
                deliveryAddress.RecipientName,
                deliveryAddress.Street,
                deliveryAddress.Street2,
                deliveryAddress.City,
                deliveryAddress.State,
                deliveryAddress.PostalCode,
                deliveryAddress.Country,
                deliveryAddress.PhoneNumber,
                null,
                defaultPaymentMethod.Id,
                defaultPaymentMethod.Name,
                "USD");

            if (products.Count > 0)
            {
                order5.AddItem(products[0].Id, orderStore.Id, products[0].Name, products[0].Price.Amount, 1, null, "Standard Shipping", 4.99m);
            }
            order5.CreateShipments();
            order5.ConfirmPayment("TXN-005-DEMO");
            order5.StartProcessing();

            foreach (var shipment in order5.Shipments)
            {
                shipment.StartProcessing();
                shipment.Ship("UPS", "1Z999AA10123456784", "https://www.ups.com/track?tracknum=1Z999AA10123456784");
            }
            order5.MarkShipped();

            await orderRepo.AddAsync(order5);

            await orderRepo.SaveChangesAsync();
        }

        // Seed sample reviews for testing the reviews display feature
        var reviewRepo = scope.ServiceProvider.GetRequiredService<IReviewRepository>();
        var storeForReviews = await storeRepo.GetBySlugAsync("demo-store");
        
        if (storeForReviews is not null)
        {
            var reviewProducts = await productRepo.GetAllByStoreIdAsync(storeForReviews.Id);
            var firstProduct = reviewProducts.FirstOrDefault();
            
            if (firstProduct is not null)
            {
                // Check if reviews already exist for this product
                var existingReviews = await reviewRepo.GetByProductIdAsync(firstProduct.Id);
                if (existingReviews.Count == 0)
                {
                    // Create additional buyer users for reviews
                    var reviewBuyerIds = new List<Guid>();
                    for (int i = 1; i <= 8; i++)
                    {
                        var reviewBuyerId = Guid.Parse($"44444444-4444-4444-4444-{i:D12}");
                        var existingReviewBuyer = await userRepo.GetByIdAsync(reviewBuyerId);
                        if (existingReviewBuyer is null)
                        {
                            var buyerEmail = Email.Create($"reviewer{i}@demo.com");
                            var reviewBuyer = new User(
                                reviewBuyerId,
                                buyerEmail,
                                passwordHasher.HashPassword("Reviewer123!"),
                                UserRole.Buyer,
                                $"Reviewer",
                                $"{(char)('A' + i - 1)}",
                                acceptedTerms: true);
                            reviewBuyer.VerifyEmail();
                            await userRepo.AddAsync(reviewBuyer);
                        }
                        reviewBuyerIds.Add(reviewBuyerId);
                    }
                    await userRepo.SaveChangesAsync();

                    // Create sample reviews with varying ratings
                    var reviewData = new[]
                    {
                        (Rating: 5, Comment: "Excellent product! Exactly what I was looking for. Fast shipping too.", DaysAgo: 2),
                        (Rating: 4, Comment: "Good quality, works as expected. Would recommend.", DaysAgo: 5),
                        (Rating: 5, Comment: "Amazing! Best purchase I've made this year.", DaysAgo: 7),
                        (Rating: 3, Comment: "It's okay, meets basic expectations but nothing special.", DaysAgo: 10),
                        (Rating: 4, Comment: "Nice product, good value for money.", DaysAgo: 14),
                        (Rating: 5, Comment: "Perfect! Five stars all the way.", DaysAgo: 21),
                        (Rating: 2, Comment: "Not quite what I expected, but it works.", DaysAgo: 30),
                        (Rating: 4, Comment: (string?)null, DaysAgo: 45)
                    };

                    var dummyOrderId = Guid.NewGuid();
                    var dummyShipmentId = Guid.NewGuid();

                    for (int i = 0; i < reviewData.Length; i++)
                    {
                        var data = reviewData[i];
                        var review = new Review(
                            dummyOrderId,
                            dummyShipmentId,
                            firstProduct.Id,
                            storeForReviews.Id,
                            reviewBuyerIds[i],
                            data.Rating,
                            data.Comment);
                        
                        // Approve the review so it's visible
                        review.Approve();
                        
                        await reviewRepo.AddAsync(review);
                    }
                    
                    await reviewRepo.SaveChangesAsync();
                }
            }
        }
    }
}
