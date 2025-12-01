using Microsoft.Extensions.DependencyInjection;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Services;
using SD.Project.Domain.Services;

namespace SD.Project.Application;

/// <summary>
/// Application layer dependency registrations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProductService>();
        services.AddScoped<ProductImageService>();
        services.AddScoped<ProductImportService>();
        services.AddScoped<ProductExportService>();
        services.AddScoped<ProductVariantService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<SearchSuggestionService>();
        services.AddScoped<RegistrationService>();
        services.AddScoped<LoginService>();
        services.AddScoped<ExternalLoginService>();
        services.AddScoped<EmailVerificationService>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<SessionService>();
        services.AddScoped<SellerOnboardingService>();
        services.AddScoped<StoreService>();
        services.AddScoped<PayoutSettingsService>();
        services.AddScoped<PayoutScheduleService>();
        services.AddScoped<SettlementService>();
        services.AddScoped<InternalUserService>();
        services.AddScoped<CartService>();
        services.AddScoped<DeliveryAddressService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<OrderService>();
        services.AddScoped<OrderExportService>();
        services.AddScoped<PartialFulfilmentService>();
        services.AddScoped<ReturnRequestService>();
        services.AddScoped<CaseMessageService>();
        services.AddScoped<SlaService>();
        services.AddScoped<PromoCodeService>();
        services.AddScoped<EscrowService>();
        services.AddScoped<RefundService>();
        services.AddScoped<PaymentWebhookService>();
        services.AddScoped<CommissionInvoiceService>();
        services.AddScoped<ShippingMethodService>();
        services.AddScoped<ShippingProviderIntegrationService>();
        services.AddScoped<ShippingLabelService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<SellerRatingService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();

        // Domain services for cart totals calculation
        services.AddSingleton<CartTotalsCalculator>();
        services.AddSingleton<CommissionCalculator>();

        // Domain services for checkout validation
        services.AddSingleton<CheckoutValidationService>();

        // Domain services for promo code validation
        services.AddSingleton<PromoCodeValidator>();

        return services;
    }
}
