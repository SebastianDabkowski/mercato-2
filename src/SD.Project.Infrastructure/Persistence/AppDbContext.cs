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
    public DbSet<DeliveryAddress> DeliveryAddresses => Set<DeliveryAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
