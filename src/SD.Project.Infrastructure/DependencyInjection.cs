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
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductImportJobRepository, ProductImportJobRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IProductVariantAttributeDefinitionRepository, ProductVariantAttributeDefinitionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserBlockInfoRepository, UserBlockInfoRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<ILoginEventRepository, LoginEventRepository>();
        services.AddScoped<ISellerOnboardingRepository, SellerOnboardingRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IPayoutSettingsRepository, PayoutSettingsRepository>();
        services.AddScoped<ISellerPayoutRepository, SellerPayoutRepository>();
        services.AddScoped<ISettlementRepository, SettlementRepository>();
        services.AddScoped<IInternalUserRepository, InternalUserRepository>();
        services.AddScoped<IInternalUserInvitationRepository, InternalUserInvitationRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IShippingRuleRepository, ShippingRuleRepository>();
        services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
        services.AddScoped<IDeliveryAddressRepository, DeliveryAddressRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IEscrowRepository, EscrowRepository>();
        services.AddScoped<ICommissionRuleRepository, CommissionRuleRepository>();
        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<ICaseMessageRepository, CaseMessageRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
        services.AddScoped<ICommissionInvoiceRepository, CommissionInvoiceRepository>();
        services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
        services.AddScoped<IShipmentStatusHistoryRepository, ShipmentStatusHistoryRepository>();
        services.AddScoped<IShippingProviderRepository, ShippingProviderRepository>();
        services.AddScoped<IShippingLabelRepository, ShippingLabelRepository>();
        services.AddScoped<ISlaConfigurationRepository, SlaConfigurationRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewReportRepository, ReviewReportRepository>();
        services.AddScoped<IReviewModerationAuditLogRepository, ReviewModerationAuditLogRepository>();
        services.AddScoped<ISellerRatingRepository, SellerRatingRepository>();
        services.AddScoped<ISellerRatingModerationAuditLogRepository, SellerRatingModerationAuditLogRepository>();
        services.AddScoped<ISellerReputationRepository, SellerReputationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IProductQuestionRepository, ProductQuestionRepository>();
        services.AddScoped<IOrderMessageRepository, OrderMessageRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IUserAnalyticsRepository, UserAnalyticsRepository>();
        services.AddScoped<ISellerDashboardRepository, SellerDashboardRepository>();
        services.AddScoped<IAnalyticsEventRepository, AnalyticsEventRepository>();
        services.AddScoped<IProductModerationAuditLogRepository, ProductModerationAuditLogRepository>();
        services.AddScoped<IPhotoModerationAuditLogRepository, PhotoModerationAuditLogRepository>();
        services.AddScoped<IVatRuleRepository, VatRuleRepository>();
        services.AddScoped<IVatRuleHistoryRepository, VatRuleHistoryRepository>();
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IDataProcessingActivityRepository, DataProcessingActivityRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<ISensitiveAccessAuditLogRepository, SensitiveAccessAuditLogRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IPaymentProviderService, PaymentProviderService>();
        services.AddScoped<IShippingProviderService, ShippingProviderService>();
        services.AddScoped<IShippingLabelStorageService, ShippingLabelStorageService>();
        services.AddScoped<IRefundProviderService, RefundProviderService>();
        services.AddScoped<IPdfGeneratorService, HtmlPdfGeneratorService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();
        services.AddScoped<ISecurityAlertService, SecurityAlertService>();
        services.AddScoped<IImageStorageService, LocalImageStorageService>();
        services.AddSingleton<ILoginRateLimiter, LoginRateLimiter>();

        // Analytics configuration and service
        services.Configure<AnalyticsOptions>(options =>
        {
            var section = configuration.GetSection("Analytics");
            options.Enabled = section.GetValue<bool>("Enabled", true);
            options.LogToConsole = section.GetValue<bool>("LogToConsole", true);
            options.PersistToDatabase = section.GetValue<bool>("PersistToDatabase", true);
        });
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}
