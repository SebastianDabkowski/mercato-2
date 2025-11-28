using Microsoft.Extensions.DependencyInjection;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Services;

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
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        return services;
    }
}
