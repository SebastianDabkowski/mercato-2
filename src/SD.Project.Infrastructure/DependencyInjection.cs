using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;
using SD.Project.Infrastructure.Repositories;
using SD.Project.Infrastructure.Services;

namespace SD.Project.Infrastructure;

/// <summary>
/// Registers infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            // TODO: replace with the database provider required by the project.
            options.UseInMemoryDatabase("AppDb");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                // Placeholder to show where provider-specific setup will live.
            }
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductImportJobRepository, ProductImportJobRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<ILoginEventRepository, LoginEventRepository>();
        services.AddScoped<ISellerOnboardingRepository, SellerOnboardingRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IPayoutSettingsRepository, PayoutSettingsRepository>();
        services.AddScoped<IInternalUserRepository, InternalUserRepository>();
        services.AddScoped<IInternalUserInvitationRepository, InternalUserInvitationRepository>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();
        services.AddScoped<ISecurityAlertService, SecurityAlertService>();
        services.AddSingleton<ILoginRateLimiter, LoginRateLimiter>();

        return services;
    }
}
