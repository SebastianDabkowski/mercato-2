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
        services.AddScoped<InternalUserService>();
        services.AddScoped<CartService>();
        services.AddScoped<DeliveryAddressService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();

        // Domain services for cart totals calculation
        services.AddSingleton<CartTotalsCalculator>();
        services.AddSingleton<CommissionCalculator>();

        return services;
    }
}
