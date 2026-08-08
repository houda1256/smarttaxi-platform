using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Referrals.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("Referrals");

        builder.HasKey(referral => referral.Id);
        builder.Property(referral => referral.Id).ValueGeneratedNever();

        builder.Property(referral => referral.ReferrerUserId).IsRequired();
        builder.HasIndex(referral => referral.ReferrerUserId);

        // Enforces "one sponsor only" at the database level, not just in the
        // application handler — defense in depth against a concurrent insert.
        builder.Property(referral => referral.RefereeUserId).IsRequired();
        builder.HasIndex(referral => referral.RefereeUserId).IsUnique();

        builder.Property(referral => referral.ReferralCodeUsed).HasMaxLength(20).IsRequired();
        builder.Property(referral => referral.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(referral => referral.Status);

        builder.Property(referral => referral.CreatedAt).IsRequired();
        builder.Property(referral => referral.UpdatedAt).IsRequired();
    }
}
