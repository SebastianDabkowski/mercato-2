using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the SellerOnboarding entity for persistence.
/// </summary>
public sealed class SellerOnboardingConfiguration : IEntityTypeConfiguration<SellerOnboarding>
{
    public void Configure(EntityTypeBuilder<SellerOnboarding> builder)
    {
        builder.ToTable("SellerOnboardings");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.HasIndex(o => o.UserId)
            .IsUnique();

        builder.Property(o => o.CurrentStep)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired();

        // Step 1: Store Profile
        builder.Property(o => o.StoreName)
            .HasMaxLength(200);

        builder.Property(o => o.StoreDescription)
            .HasMaxLength(2000);

        builder.Property(o => o.StoreAddress)
            .HasMaxLength(500);

        builder.Property(o => o.StoreCity)
            .HasMaxLength(100);

        builder.Property(o => o.StorePostalCode)
            .HasMaxLength(20);

        builder.Property(o => o.StoreCountry)
            .HasMaxLength(100);

        builder.Property(o => o.StoreProfileCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Step 2: Verification Data
        builder.Property(o => o.BusinessName)
            .HasMaxLength(200);

        builder.Property(o => o.BusinessRegistrationNumber)
            .HasMaxLength(50);

        builder.Property(o => o.TaxIdentificationNumber)
            .HasMaxLength(50);

        builder.Property(o => o.BusinessAddress)
            .HasMaxLength(500);

        builder.Property(o => o.VerificationCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Step 3: Payout Settings
        builder.Property(o => o.BankAccountHolder)
            .HasMaxLength(200);

        builder.Property(o => o.BankAccountNumber)
            .HasMaxLength(50);

        builder.Property(o => o.BankName)
            .HasMaxLength(200);

        builder.Property(o => o.BankSwiftCode)
            .HasMaxLength(20);

        builder.Property(o => o.PayoutCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Timestamps
        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();

        builder.Property(o => o.SubmittedAt);

        builder.Property(o => o.VerifiedAt);
    }
}
