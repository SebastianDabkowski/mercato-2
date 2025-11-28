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
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<LoginEvent> LoginEvents => Set<LoginEvent>();
    public DbSet<SellerOnboarding> SellerOnboardings => Set<SellerOnboarding>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<PayoutSettings> PayoutSettings => Set<PayoutSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
