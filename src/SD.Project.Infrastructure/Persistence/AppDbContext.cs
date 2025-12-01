using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence;

/// <summary>
/// EF Core database context used for persistence.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantAttributeDefinition> ProductVariantAttributeDefinitions => Set<ProductVariantAttributeDefinition>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<LoginEvent> LoginEvents => Set<LoginEvent>();
    public DbSet<SellerOnboarding> SellerOnboardings => Set<SellerOnboarding>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<PayoutSettings> PayoutSettings => Set<PayoutSettings>();
    public DbSet<InternalUser> InternalUsers => Set<InternalUser>();
    public DbSet<InternalUserInvitation> InternalUserInvitations => Set<InternalUserInvitation>();
    public DbSet<ProductImportJob> ProductImportJobs => Set<ProductImportJob>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ShippingRule> ShippingRules => Set<ShippingRule>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<DeliveryAddress> DeliveryAddresses => Set<DeliveryAddress>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderShipment> OrderShipments => Set<OrderShipment>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PromoCodeUsage> PromoCodeUsages => Set<PromoCodeUsage>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnRequestItem> ReturnRequestItems => Set<ReturnRequestItem>();
    public DbSet<CaseMessage> CaseMessages => Set<CaseMessage>();
    public DbSet<EscrowPayment> EscrowPayments => Set<EscrowPayment>();
    public DbSet<EscrowAllocation> EscrowAllocations => Set<EscrowAllocation>();
    public DbSet<EscrowLedger> EscrowLedgers => Set<EscrowLedger>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<SellerPayout> SellerPayouts => Set<SellerPayout>();
    public DbSet<SellerPayoutItem> SellerPayoutItems => Set<SellerPayoutItem>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<SettlementItem> SettlementItems => Set<SettlementItem>();
    public DbSet<SettlementAdjustment> SettlementAdjustments => Set<SettlementAdjustment>();
    public DbSet<CommissionInvoice> CommissionInvoices => Set<CommissionInvoice>();
    public DbSet<CommissionInvoiceLine> CommissionInvoiceLines => Set<CommissionInvoiceLine>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<CreditNoteLine> CreditNoteLines => Set<CreditNoteLine>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<ShipmentStatusHistory> ShipmentStatusHistories => Set<ShipmentStatusHistory>();
    public DbSet<ShippingProvider> ShippingProviders => Set<ShippingProvider>();
    public DbSet<ShippingLabel> ShippingLabels => Set<ShippingLabel>();
    public DbSet<SlaConfiguration> SlaConfigurations => Set<SlaConfiguration>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewReport> ReviewReports => Set<ReviewReport>();
    public DbSet<ReviewModerationAuditLog> ReviewModerationAuditLogs => Set<ReviewModerationAuditLog>();
    public DbSet<SellerRating> SellerRatings => Set<SellerRating>();
    public DbSet<SellerRatingModerationAuditLog> SellerRatingModerationAuditLogs => Set<SellerRatingModerationAuditLog>();
    public DbSet<SellerReputation> SellerReputations => Set<SellerReputation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();
    public DbSet<OrderMessage> OrderMessages => Set<OrderMessage>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<UserBlockInfo> UserBlockInfos => Set<UserBlockInfo>();
    public DbSet<ProductModerationAuditLog> ProductModerationAuditLogs => Set<ProductModerationAuditLog>();
    public DbSet<PhotoModerationAuditLog> PhotoModerationAuditLogs => Set<PhotoModerationAuditLog>();
    public DbSet<VatRule> VatRules => Set<VatRule>();
    public DbSet<VatRuleHistory> VatRuleHistories => Set<VatRuleHistory>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<DataProcessingActivity> DataProcessingActivities => Set<DataProcessingActivity>();
    public DbSet<DataProcessingActivityAuditLog> DataProcessingActivityAuditLogs => Set<DataProcessingActivityAuditLog>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SensitiveAccessAuditLog> SensitiveAccessAuditLogs => Set<SensitiveAccessAuditLog>();
    public DbSet<ConsentType> ConsentTypes => Set<ConsentType>();
    public DbSet<ConsentVersion> ConsentVersions => Set<ConsentVersion>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<UserConsentAuditLog> UserConsentAuditLogs => Set<UserConsentAuditLog>();
    public DbSet<UserDataExport> UserDataExports => Set<UserDataExport>();
    public DbSet<AccountDeletionRequest> AccountDeletionRequests => Set<AccountDeletionRequest>();
    public DbSet<AccountDeletionAuditLog> AccountDeletionAuditLogs => Set<AccountDeletionAuditLog>();
    public DbSet<CriticalActionAuditLog> CriticalActionAuditLogs => Set<CriticalActionAuditLog>();
    public DbSet<SecurityIncident> SecurityIncidents => Set<SecurityIncident>();
    public DbSet<SecurityIncidentStatusHistory> SecurityIncidentStatusHistories => Set<SecurityIncidentStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
