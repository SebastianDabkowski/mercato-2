using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<RegistrationService>();
        services.AddScoped<LoginService>();
        services.AddScoped<ExternalLoginService>();
        services.AddScoped<EmailVerificationService>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<SessionService>();
        return services;
    }
}
